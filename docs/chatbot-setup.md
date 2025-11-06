# Chatbot Ollama Setup

Sigue estos pasos para que el asistente use un modelo local gratuito con [Ollama](https://ollama.com/):

1. **Instala Ollama** en tu equipo (Windows/Mac/Linux). Descárgalo desde la web oficial e instálalo con la configuración por defecto.
2. **Inicia un modelo compatible**. Por ejemplo, la variante ligera de Mistral:

	```pwsh
	ollama run mistral
	```

	La primera ejecución descargará ~4 GB. Cuando veas el prompt interactivo puedes salir (`Ctrl+C`); el servidor seguirá disponible en `http://localhost:11434`.

3. **Opcional: define un prompt del sistema y el modelo por defecto** mediante user-secrets (no se requiere para usar los valores de `appsettings.json`):

	```pwsh
	dotnet user-secrets set "Chatbot:Ollama:Model" "mistral"
	dotnet user-secrets set "Chatbot:Ollama:SystemPrompt" "Actúa como un asistente de videojuegos en español..."
	```

4. **Reinicia la aplicación** para que los cambios de configuración se apliquen:

	```pwsh
	dotnet watch run
	```

5. **Verifica el estado**: abre el chatbot en la web y haz preguntas. Si Ollama está en marcha, las respuestas mostrarán `Source: hybrid`. Si el servidor local está caído, el sistema volverá al fallback local automático.

> Nota: puedes ajustar la URL (`Chatbot:Ollama:BaseUrl`) si corres Ollama en otra máquina accesible por la red.
