using System.Numerics;
using Novolis.Math.Geometry;

namespace Novolis.Rendering.TwoD;

/// <summary>Walks a <see cref="TwoDScene"/> into a host-neutral <see cref="PlanarDrawList"/>.</summary>
public static class TwoDSceneTessellator
{
    /// <summary>Tessellates sprites, polygons, HUD, and menus into screen-space triangles.</summary>
    public static PlanarDrawList Tessellate(TwoDScene scene, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(scene);
        scene.Camera.ViewportWidth = System.Math.Max(1, width);
        scene.Camera.ViewportHeight = System.Math.Max(1, height);

        var triangles = new List<PlanarTriangle>(256);
        var usedTextures = new HashSet<int>();

        foreach (var sprite in scene.Sprites.OrderBy(s => s.Layer).ThenBy(s => s.SortKey))
        {
            if (sprite.ScreenSpace)
            {
                continue;
            }

            EmitWorldSprite(scene, sprite, triangles, usedTextures);
        }

        foreach (var anim in scene.AnimatedSprites.OrderBy(a => a.Layer).ThenBy(a => a.SortKey))
        {
            if (anim.Clip is null)
            {
                continue;
            }

            var rect = anim.Clip.GetSourceRect(anim.CurrentFrameIndex);
            EmitWorldQuad(scene, anim.Clip.Sheet.Texture, rect, anim.Transform, Rgba32.White, triangles, usedTextures);
        }

        foreach (var poly in scene.StaticPolygons.OrderBy(p => p.Layer).ThenBy(p => p.SortKey))
        {
            EmitPolygon(scene, poly, triangles, usedTextures);
        }

        foreach (var element in scene.Hud.Elements)
        {
            switch (element)
            {
                case TwoDHudText text:
                    TwoDBitmapGlyphs.EmitText(triangles, text.Text, text.ScreenX, text.ScreenY, text.Scale, text.Color);
                    break;
                case TwoDHudSprite icon:
                    EmitScreenTexturedQuad(icon.Texture, icon.Source, icon.ScreenX, icon.ScreenY, icon.Width, icon.Height, icon.Tint, triangles, usedTextures);
                    break;
            }
        }

        if (scene.Menus.Active is { } menu)
        {
            var vw = scene.Camera.ViewportWidth;
            var vh = scene.Camera.ViewportHeight;
            if (menu.DimBackground)
            {
                EmitScreenSolidQuad(0, 0, vw, vh, new Rgba32(0, 0, 0, 210), triangles);
            }

            var titleY = vh * 0.25f;
            TwoDBitmapGlyphs.EmitText(triangles, menu.Title, vw * 0.5f - menu.Title.Length * 6f, titleY, 3f, Rgba32.White);
            var itemY = titleY + 80f;
            for (var i = 0; i < menu.Items.Count; i++)
            {
                var prefix = i == menu.FocusIndex ? "> " : "  ";
                var color = i == menu.FocusIndex ? Rgba32.Chartreuse : Rgba32.White;
                TwoDBitmapGlyphs.EmitText(triangles, prefix + menu.Items[i].Label, vw * 0.5f - 80f, itemY + i * 32f, 2.5f, color);
            }
        }

        var blobs = new List<PlanarTextureBlob>(usedTextures.Count);
        foreach (var id in usedTextures.OrderBy(v => v))
        {
            var handle = new TwoDTextureId(id);
            if (!scene.Textures.Contains(handle))
            {
                continue;
            }

            var info = scene.Textures.GetInfo(handle);
            var pixels = new Rgba32[info.Width * info.Height];
            scene.Textures.CopyPixels(handle, pixels, out _, out _);
            blobs.Add(new PlanarTextureBlob(id, info.Width, info.Height, pixels));
        }

        return new PlanarDrawList
        {
            ViewportWidth = scene.Camera.ViewportWidth,
            ViewportHeight = scene.Camera.ViewportHeight,
            ClearColor = scene.Camera.ClearColor,
            Textures = [.. blobs],
            Triangles = [.. triangles],
        };
    }

    private static void EmitWorldSprite(
        TwoDScene scene,
        TwoDSpriteInstance sprite,
        List<PlanarTriangle> triangles,
        HashSet<int> usedTextures) =>
        EmitWorldQuad(scene, sprite.Texture, sprite.SourceRect, sprite.Transform, sprite.Tint, triangles, usedTextures);

    private static void EmitWorldQuad(
        TwoDScene scene,
        TwoDTextureId texture,
        TwoDSourceRect source,
        TwoDTransform transform,
        Rgba32 tint,
        List<PlanarTriangle> triangles,
        HashSet<int> usedTextures)
    {
        if (!texture.IsValid)
        {
            return;
        }

        var w = transform.Scale.X;
        var h = transform.Scale.Z;
        var cx = transform.Position.X;
        var cz = transform.Position.Z;
        var u0 = source.U0;
        var v0 = source.V0;
        var u1 = source.U1;
        var v1 = source.V1;
        if (transform.FlipX)
        {
            (u0, u1) = (u1, u0);
        }

        var corners = new Vector3[]
        {
            new(-w * 0.5f, 0f, -h * 0.5f),
            new(w * 0.5f, 0f, -h * 0.5f),
            new(-w * 0.5f, 0f, h * 0.5f),
            new(w * 0.5f, 0f, h * 0.5f),
        };
        var rot = Matrix4x4.CreateRotationY(transform.RotationY);
        for (var i = 0; i < corners.Length; i++)
        {
            var local = Vector3.Transform(corners[i], rot);
            var world = new Vector3(local.X + cx, 0f, local.Z + cz);
            corners[i] = scene.Camera.WorldToScreen(world);
        }

        usedTextures.Add(texture.Value);
        AddQuad(
            triangles,
            corners[0].X,
            corners[0].Z,
            corners[1].X,
            corners[1].Z,
            corners[2].X,
            corners[2].Z,
            corners[3].X,
            corners[3].Z,
            u0,
            v1,
            u1,
            v1,
            u0,
            v0,
            u1,
            v0,
            tint,
            texture.Value);
    }

    private static void EmitPolygon(
        TwoDScene scene,
        TwoDStaticPolygon poly,
        List<PlanarTriangle> triangles,
        HashSet<int> usedTextures)
    {
        if (poly.DrawFilled)
        {
            var textured = poly.Texture.IsValid;
            var texId = textured ? poly.Texture.Value : 0;
            var meters = poly.TextureMeters <= 0f ? 1f : poly.TextureMeters;
            if (textured)
            {
                usedTextures.Add(texId);
            }

            foreach (var face in poly.Shape.FacesSpan)
            {
                var a = scene.Camera.WorldToScreen(face.A);
                var b = scene.Camera.WorldToScreen(face.B);
                var c = scene.Camera.WorldToScreen(face.C);
                var ua = textured ? face.A.X / meters : 0f;
                var va = textured ? face.A.Z / meters : 0f;
                var ub = textured ? face.B.X / meters : 0f;
                var vb = textured ? face.B.Z / meters : 0f;
                var uc = textured ? face.C.X / meters : 0f;
                var vc = textured ? face.C.Z / meters : 0f;
                triangles.Add(new PlanarTriangle(
                    new PlanarVertex(a.X, a.Z, ua, va, poly.FillColor),
                    new PlanarVertex(b.X, b.Z, ub, vb, poly.FillColor),
                    new PlanarVertex(c.X, c.Z, uc, vc, poly.FillColor),
                    texId));
            }
        }

        if (poly.DrawOutline)
        {
            foreach (var edge in poly.Shape.EdgesSpan)
            {
                EmitWorldLine(scene, edge.A, edge.B, 0.05f, poly.OutlineColor, triangles);
            }
        }
    }

    private static void EmitWorldLine(
        TwoDScene scene,
        Vector3 a,
        Vector3 b,
        float thickness,
        Rgba32 color,
        List<PlanarTriangle> triangles)
    {
        var dir = b - a;
        dir.Y = 0f;
        if (dir.LengthSquared() < 1e-8f)
        {
            return;
        }

        dir = Vector3.Normalize(dir);
        var perp = new Vector3(-dir.Z, 0f, dir.X) * thickness * 0.5f;
        var a0 = scene.Camera.WorldToScreen(a + perp);
        var b0 = scene.Camera.WorldToScreen(b + perp);
        var b1 = scene.Camera.WorldToScreen(b - perp);
        var a1 = scene.Camera.WorldToScreen(a - perp);
        triangles.Add(new PlanarTriangle(
            new PlanarVertex(a0.X, a0.Z, 0f, 0f, color),
            new PlanarVertex(b0.X, b0.Z, 0f, 0f, color),
            new PlanarVertex(b1.X, b1.Z, 0f, 0f, color),
            0));
        triangles.Add(new PlanarTriangle(
            new PlanarVertex(a0.X, a0.Z, 0f, 0f, color),
            new PlanarVertex(b1.X, b1.Z, 0f, 0f, color),
            new PlanarVertex(a1.X, a1.Z, 0f, 0f, color),
            0));
    }

    private static void EmitScreenTexturedQuad(
        TwoDTextureId texture,
        TwoDSourceRect source,
        float x,
        float y,
        float width,
        float height,
        Rgba32 tint,
        List<PlanarTriangle> triangles,
        HashSet<int> usedTextures)
    {
        if (!texture.IsValid)
        {
            return;
        }

        usedTextures.Add(texture.Value);
        AddQuad(
            triangles,
            x,
            y,
            x + width,
            y,
            x,
            y + height,
            x + width,
            y + height,
            source.U0,
            source.V1,
            source.U1,
            source.V1,
            source.U0,
            source.V0,
            source.U1,
            source.V0,
            tint,
            texture.Value);
    }

    private static void EmitScreenSolidQuad(float x, float y, float w, float h, Rgba32 color, List<PlanarTriangle> triangles) =>
        AddQuad(triangles, x, y, x + w, y, x, y + h, x + w, y + h, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, color, 0);

    private static void AddQuad(
        List<PlanarTriangle> triangles,
        float x0,
        float y0,
        float x1,
        float y1,
        float x2,
        float y2,
        float x3,
        float y3,
        float u0,
        float v0,
        float u1,
        float v1,
        float u2,
        float v2,
        float u3,
        float v3,
        Rgba32 color,
        int textureId)
    {
        triangles.Add(new PlanarTriangle(
            new PlanarVertex(x0, y0, u0, v0, color),
            new PlanarVertex(x2, y2, u2, v2, color),
            new PlanarVertex(x1, y1, u1, v1, color),
            textureId));
        triangles.Add(new PlanarTriangle(
            new PlanarVertex(x1, y1, u1, v1, color),
            new PlanarVertex(x2, y2, u2, v2, color),
            new PlanarVertex(x3, y3, u3, v3, color),
            textureId));
    }
}
