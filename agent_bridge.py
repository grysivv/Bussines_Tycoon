import requests
from antigravity_sdk import skill

@skill.register("local_ollama_bridge")
def query_ollama(prompt: str):
    """Przekazuje zapytanie do Twojego lokalnego modelu Ollama."""
    url = "http://localhost:11434/api/generate"
    payload = {
        "model": "qwen2.5-coder:7b",
        "prompt": prompt,
        "stream": False
    }
    
    response = requests.post(url, json=payload)
    if response.status_code == 200:
        return response.json().get("response")
    else:
        return f"Error: {response.text}"

# Rejestracja umiejętności w IDE
skill.start_bridge()