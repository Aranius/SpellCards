# D&D 3.5 Spell Cards (MTG size) PDF Generator

## Run
- Edit `requests.txt` (one spell name per line, `#` comments allowed)
- Build & run:

```bash
dotnet run --project Dnd35.SpellCards
```

A PDF named `out/spellcards.pdf` will be produced next to the executable.

Icons provided by https://game-icons.net 
Licensed under CC BY 3.0

## Optional: Automatic Rules-Only Summaries
The generator now defaults to the higher-quality `mixtral:8x7b` model served by Ollama.

1. Install [Ollama](https://ollama.com/) and run `ollama pull mixtral:8x7b` (or any other model you prefer).
2. Start the Ollama service (`ollama serve`) so it listens on `http://127.0.0.1:11434`.
3. Adjust `settings.json` (copied next to the executable) if you want a different default:

```json
{
  "ollama": {
    "model": "mixtral:8x7b",
    "endpoint": "http://127.0.0.1:11434"
  }
}
```

4. Run the spell-card generator; `[condense]` logs show when summaries are produced and cached under `cache/condensed`.
5. Override per-run behavior with command-line switches:
   - `--model=phi3:mini` (forces a specific model)
   - `--endpoint=http://localhost:11434`
   - `--no-condense` (skip the AI summary phase)
   Environment variables `SPELLCARDS_OLLAMA_MODEL` / `SPELLCARDS_OLLAMA_ENDPOINT` remain available as a fallback.

> When switching models, delete `cache/condensed` so summaries regenerate with the new backend.

> The Ollama step is optional; if the service is unavailable the app automatically falls back to the original SRD text.