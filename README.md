# Kaput
## Project overview
Kaput is a web application made by team Modemas, using ASP.NET (C# NET 8.0) Core and React (Javascript).

## Team members
* Arnas Vyšniauskas [Arvys7](https://github.com/Arvys7)
* Audrius Duoblys [daudrius](https://github.com/daudrius)
* Greta Mazuraitytė [Gretam16](https://github.com/Gretam16)
* Jonas Grubliauskas [someguy0555](https://github.com/someguy0555)
* Klaidas Blandis [KlaidasBlandis](https://github.com/KlaidasBlandis)

## List of functionalities
* **Alpha**:
    - The ability to join/create a lobby.
    - The ability to submit quiz topics.
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
* Docker (Docker Desktop or just cli Docker).

## Building
1. Open a terminal in project root.
2. 'cd' to Modemas.Server
3. 'cd' to Docker
4. Start docker container.
3. Run:
    ```bash
    dotnet build
    dotnet run
    ```
    This command starts both the backend and the frontend.

## Report reading
There should be scripts for that, whether .bat or .sh
To run tests, simply run those scripts.
* full-test.sh
* full-test.bat

## Video
[Link to the final version video](https://youtu.be/4C2WSj65iL4?si=8ZjtPuj1J5BgS72M)
