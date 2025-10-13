# Container
This container contains our LLM, which we'll use to generate questions.

## Prerequisites
* You have docker cli.

## How to run this thing?
If you want to build this docker container manually, do the following:
* ` cd ./Modemas.Server/Docker.` This assumes that you are inside the project directory (i.e. `Modemas`)
* Then, enter the following command: `docker compose up -d <SPECIFIC_OLLAMA_VERSION> --build`, which will build your container and run it.
NOTE: `<SPECIFIC_OLLAMA_VERSION>` is a specific ollama version. At the time of writing, there are three versions: ollama-cpu, ollama-nvidia and ollama-amd.
    I only tested ollama-amd, so if there could be issues with the other versions.
* If this is the first time you're building the container or you want to build our LLM with a new Modelfile, do this:
    * Go inside the container with: `docker exec -it <SPECIFIC_OLLAMA_VERSION> bash`. This let's you interact with the shell inside the container.
    * Inside the container shell, run `ollama create deepseek -f /home/Modelfile`. This will "build " deepseek using the instructions located in the specified Modelfile.
    * Exit the container with `exit`.
* Everything should now hopefully work.
