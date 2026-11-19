Transport Tycoon

A transport-management simulation game written in C# with WPF, developed as a team project for the Software Technology course at ELTE Faculty of Informatics.

The player builds roads, buys vehicles and sets up routes between cities and industries. The game includes BFS-based pathfinding for road generation, traffic rules at intersections, and saving and loading games.

The project follows the MVVM architecture:
- TransportTycoon_BOSZI: game model and persistence
- transport_tycoon_viewlayer: WPF user interface
- TransportTycoon_BOSZI_TESTS: unit tests (xUnit)

How to run (Windows, .NET 9 SDK required):
dotnet run --project transport_tycoon_viewlayer

How to run the tests:
dotnet test

My contribution: traffic logic, pathfinding

Graphics: isometric tiles by Kenney (kenney.nl), CC0 license.
