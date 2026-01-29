# Virtualni avatar za upravljanje robotom
Virtualni avatar za upravljanje robotom na ROS2 platformi.

Rad u sklopu MetaRoboLearn projekta.

Robot omogućuje čovjekoliku interakciju, kao i upravljanje robotom, s obzirom da koristi veliki jezični model.
Cilj je bio omogućiti korištenje prirodnog jezika za upravljanje robotom u obrazovne svrhe.

Nakon uspješnog pokretanja aplikacije, za početak interakcije s avatarom, korisnik mora kliknuti na tipku mikrofona na ekranu,
nakon čega započinje snimanje. Nakon što je korisnik izgovorio sve što je htio, pritiskom na mikrofon on zaustavlja
snimanje i dobiva povratnu informaciju prikazanu na ekranu koja obavještava da se čeka
na odgovor poslužitelja. Korisnik ako postoji zapis o naredbi za robota u svakom trenutku može izvršiti akciju slanja na robot.

## Instalacija

### Unity Game Engine
Srce ovog projekta je Unity, višeplatformski softver za izradu igara. Unity se koristi za pokretanje avatara u ispitnom i razvojnom okruženju. 

Koraci za instalaciju Unity okruženja:
1. Kreirati [Unity ID](https://id.unity.com/en/conversations/4e95f832-fd5f-418b-a268-a309a113aae6005f) račun
2. Instalirati [Unity Hub](https://unity.com/download)
3. Instalirati [Unity Editor 2022.3.50.f1](unityhub://2022.3.50f1/c3db7f8bf9b1)

### Visual Studio (Windows)
Visual Studio je integrirano razvojno okruženje (IDE). Koristi se za razvoj i pisanje koda kompatibilnog s Unity okruženjem.

Koraci za instalaciju Visual Studio okruženja:
1. Instalirati [Visual Studio Installer](https://visualstudio.microsoft.com/downloads/)
2.  Odabrati jednu od verzija po želji
3. Prije same instalacije, odabrati iduće pakete:
	- .NET desktop development
	- Dekstop development with C++
	- Game development with Unity
4. Pokrenuti instalaciju

### Ollama
* preuzeti Ollama sa [poveznice](https://ollama.com)
* prezueti [llama3.1:8b] model unosom u konzolu
```
ollama run llama3.1:8b
```

### TTS
TTS je biblioteka za generiranje teksta u govor koja se ispituje na Ubuntu operacijskom sustavu.

Upute za instalaciju TTS biblioteke na Windows operacijskom sustavu:
1. Instalirati [Python](https://www.python.org/downloads/) verziju **>= 3.9, < 3.12**, tijekom instalacije odabrati:
	- Install launcher for all users
	- Add Python _X.xx_ to PATH
2. Otvoriti naredbeni redak te se pozicionirati u direktorij po želji
3. Kreirati Python virtualno okruženje nardebom
``` 
python.exe -m venv. 
```
4. Pozicionirat se u direktorij ``Scripts``
```
cd .\Scripts\
```
5. Pokrenuti skriptu nardebom
```
.\activate
```
_U slučaju da vas zatkne greška s tekstom:_
```
...cannot be loaded because the execution of scripts is disabled on this system
```
_Pokrenite novi naredbeni redak uz opciju **Run as Administrator** te unesite_
```
set-executionpolicy remotesigned
```
6. Instalirati paket _Wheel_ nardebom
```
pip install wheel
```
7. Instalirati paket _TTS_ naredbom _(veliki paket je u pitanju, očekivano je da potraje)_
```
pip install TTS
```

### STT
* skinuti željeni model weight s [poveznice](https://huggingface.co/ggerganov/whisper.cpp/tree/main) (preporuka koristiti ggml-base.bin)
* staviti .bin datoteku u venv direktorij
* model se bira kao varijabla u "server-dipl.py"

* više o veličinama modela na [OpenAi readme](https://github.com/openai/whisper#available-models-and-languages)

## FFmpeg
* na računalu mora biti instaliran FFmpeg kako bi se transkripcija govora mogla obaviti (https://www.ffmpeg.org/)

## Modeli
* **tts_models/en/ljspeech/glow-tts** --> verzija ženskastog/mješovitog TTS-a, najbolje zbog kombinacije kvalitete zvuka i brzine
* **tts_models/en/jenny/jenny** --> lijepi, ali spor engleski ženski glas
* **tts_models/en/blizzard2013/capacitron-t2-c50** --> radi s emocijama, ali ne dobro baš
* model se bira kao varijabla u "server-dipl.py"

## Build
* za buildanje WebGl verzije unutar Unity-a: File > Build Settings i izabrati WebGL kao ciljnu platformu, pokrenuti Build i zatim ga poslužiti kao statične datoteke (npr. python HTTP server)

## Pokretanje
* obavezno pokrenuti **Ollama** aplikaciju (misli se na Ollama executable, ne na pokretanje modela unutar konzole), te Python skriptu "**server-dipl.py**" **UNUTAR** virtualnog okruženja, poslužiti statične datoteke nastale buildom (npr. Python HTTP server) i koristiti aplikaciju

## Interakcija
* po potrebi nakon pokretanja WebGL aplikacije korigirati adresu servera i samog robota u postavkama u gornjem desnom kutu
