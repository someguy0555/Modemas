FROM ollama/ollama:latest

COPY ./models/questionProvider/Modelfile /home/Modelfile

# RUN ollama serve & \
#     sleep 5 && \
#     ollama create deepseek -f /home/Modelfile && \
#     pkill ollama

EXPOSE 11434

CMD ["serve"]
