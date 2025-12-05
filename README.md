<img width="128" height="128" alt="DivisionEngineLogoR" src="https://github.com/user-attachments/assets/4495b5e3-012d-42e4-8e04-1eecbf22ee1d" />

# Division Engine

Division Engine is an SDF-based game engine written entirely in C#. Utilizing Avalonia UI for the interface and Silk.NET for native rendering, Division Engine features a comprehensive build pipeline that dynamically builds HLSL shader code from .NET code, thanks to a library called ComputeSharp.

*Note: This engine is still in preview and has known issues; it is specifically for experimentation and education only.*

The render pipeline is built using an OpenGL backend with HLSL shaders written in C# using ComputeSharp.

Picture this:
- SDF-based rendering
- GPU compute acceleration in C#
- Open source
- ECS backend, fast data handling
- Convenient editor tooling

Editor Mockup (*Preview*):
<img width="1919" height="1032" alt="Screenshot 2025-12-01 163210" src="https://github.com/user-attachments/assets/01b89a41-aa90-4db8-871c-c42fd6083751" />

## What Are SDFs?

*Signed Distance Fields* are spatial fields that store information represented as a grid sampling of the closest distance to the surface of an object defined as a polygonal model. Usually, the convention of using negative values inside the object and positive values outside the object is applied. Signed distance fields are important in computer graphics and related fields. Often, they are used for collision detection in cloth animation, soft-body physics effects, malleable geometry, volumetric effects, and fluid simulation.
(https://developer.nvidia.com/gpugems/gpugems3/part-v-physics-simulation/chapter-34-signed-distance-fields-using-single-pass-gpu)

## How to Work with ECS

*ECS* or an entity-component-system framework is a way of organizing game data such that it is memory efficient and hyper-performant. Entities are simply IDs with components stored as a dictionary in an "ECS World" object. Systems are code files written that operate on an awake --> update --> fixed update --> render schedule, allowing components to be manipulated during different engine loops/stages. For more information on ECS, check out how the Unity game engine implemented its ECS framework here: https://unity.com/ecs

### Resources:
Follow the development: https://trello.com/b/mWtyHBMf/division-engine

Tutorials by Inigo Quilez (Not sponsored, just useful for learning constructive geometry):
- Build mathematical worlds: https://youtu.be/0ifChJ0nJfM?si=ypKU1rz-8JloPlj2
- Build a 3D landscape: https://youtu.be/BFld4EBO2RE?si=EASXvq-ez2qBOIHN
- Paint a 3D character with math: https://youtu.be/8--5LwHRhjk?si=fH9QwvCz6dLptHE1

## Framework

Division Engine is built using three core packages: Silk.NET, ComputeSharp, and AvaloniaUI.
Check them out here:
- [Silk.NET](https://github.com/dotnet/Silk.NET)
- [ComputeSharp](https://github.com/Sergio0694/ComputeSharp)
- [AvaloniaUI](https://github.com/AvaloniaUI/Avalonia)
