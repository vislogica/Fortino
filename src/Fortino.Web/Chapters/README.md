# Chapters — il codice dei capitoli

Una cartella per capitolo del libro, per esempio `Ch07.BusinessLogic/`. Dentro:

- `Vulnerable/` il codice vulnerabile;
- `Secure/` il codice corretto;
- `Scenarios/` gli attacchi dimostrativi (classi che implementano `ILabCase`);
- `ChNNModule.cs` il modulo del capitolo (implementa `ILabChapterModule`): registra servizi,
  indirizzi `/lab/chNN/vulnerable/...` e `/lab/chNN/secure/...` ed eventuali tabelle;
- `chNN.http` le richieste da inviare a mano da Visual Studio.

I pezzi di codice stampati nel libro sono racchiusi tra `#region libro: nome` e `#endregion`.
Il Laboratorio li mostra a video e lo script del libro li copia nel testo.

Regola: il codice vulnerabile esiste **solo** nelle cartelle `Vulnerable/`. Il resto
dell'applicazione è scritto nel modo corretto.
