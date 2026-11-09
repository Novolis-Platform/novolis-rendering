using Novolis.Rendering.Backends.Igpu;

using var backend = new IlgpuRayTracingBackend();
Console.WriteLine(backend.BackendLabel);
