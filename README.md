<img width="128" height="128" alt="DivisionEngineLogoR" src="https://github.com/user-attachments/assets/4495b5e3-012d-42e4-8e04-1eecbf22ee1d" />

# Division Engine

Division Engine is an SDF-based game engine written entirely in C#. Utilizing Avalonia UI for the interface and Silk.Net for native rendering, Division Engine has a complete build pipeline that is entirely written in C#.

The render pipeline is built using an OpenGL backend with HLSL shaders written in C# using ComputeSharp.

Picture this:
- SDF-based rendering
- GPU compute acceleration in C#
- Open source
- Simple tooling

## What Are SDFs?

*Signed Distance Fields* are spatial fields that store information represented as a grid sampling of the closest distance to the surface of an object defined as a polygonal model. Usually, the convention of using negative values inside the object and positive values outside the object is applied. Signed distance fields are important in computer graphics and related fields. Often, they are used for collision detection in cloth animation, soft-body physics effects, malleable geometry, volumetric effects, and fluid simulation.
(https://developer.nvidia.com/gpugems/gpugems3/part-v-physics-simulation/chapter-34-signed-distance-fields-using-single-pass-gpu)

## How to Work with ECS

*ECS* or an entity-component-system framework is a way of organizing game data such that it is memory efficient and hyper-performant. Entities are simply IDs with components stored as a dictionary in an "ECS World" object. Systems are then the code that is written that operates on an awake --> update schedule, allowing components to be manipulated over time. For more information on ECS, check out how the Unity game engine implemented its ECS framework here: https://unity.com/ecs

### Resources:
Follow the development: https://trello.com/b/mWtyHBMf/division-engine

Tutorials:
- Build mathematical worlds: https://www.youtube.com/watch?v=0ifChJ0nJfM&list=PL0EpikNmjs2CYUMePMGh3IjjP4tQlYqji
- Build a 3D landscape: https://www.youtube.com/watch?v=BFld4EBO2RE&t=1190s

## Framework

Division Engine is built using three core packages: Silk.Net, ComputeSharp, and AvaloniaUI.
Check them out here:
- [Silk.Net](https://github.com/dotnet/Silk.NET)
- [ComputeSharp](https://github.com/Sergio0694/ComputeSharp)
- [AvaloniaUI](https://github.com/AvaloniaUI/Avalonia)
