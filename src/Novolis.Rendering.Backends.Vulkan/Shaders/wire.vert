#version 450
layout(location = 0) in vec3 inPos;
layout(location = 1) in vec4 inColor;
layout(push_constant) uniform PushConstants {
    mat4 mvp;
} pc;
layout(location = 0) out vec4 vColor;
void main() {
    // Flip Y for Vulkan NDC (System.Numerics projection is OpenGL-style).
    vec4 clip = pc.mvp * vec4(inPos, 1.0);
    clip.y = -clip.y;
    gl_Position = clip;
    vColor = inColor;
}
