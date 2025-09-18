# Kauput
## Project overview
Kauput is a web application made by team Modemas, using ASP.NET (C# NET 8.0) Core and React (Javascript).

## Team members
* Arnas Vyšniauskas [Arvys7](https://github.com/Arvys7)
* Audrius Duoblys
* Greta Mazuraitytė
* Jonas Grubliauskas [someguy0555](https://github.com/someguy0555)
* Klaidas Blandis

## List of functionalities
* **Alpha**:
    - The ability to join/create a lobby.
    - The ability to submit quiz topics.
    - The ability to vote for topics.
    - The ability to be able to choose a topic, from which questions will be generated.
    - The ability to answer questions.
    - Very basic UI
* **Beta**:
    - Database will store questions.
    - Timer
    - Point system
    - Fix any bugs
    - Somewhat normal UI
* **Final**:
    - Leaderboard
    - Polish existing features
    - Fix any remaining bugs
    - Fully feature-complete UI

## Tools
* ASP.NET Core
    - SignalR (built-in)
* React (Javascript)
    - Vite

## Project structure
The project consists of two parts:
* **Modemas.Server** - This is the Backend and it uses .NET.
* **modemas.client** - This is the Frontend and it uses React.

## Prerequisites
* .NET 8.0 SDK
* Node.js and npm

## Building
1. Open a terminal in project root.
2. 'cd' to Modemas.Server
3. Run:
    ```bash
    dotnet build
    dotnet run
    ```
    This command starts both the backend and the frontend.

