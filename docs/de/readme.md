# EasyEPLANner - Open Source

### Repository
Dieses Repository ist ein Open-Source Projekt - EasyEPLANner.
Wir arbeiten am Projekt und lösen verschiedene mit der Entwicklung des Projekts verbundene Aufgaben.

### EPLAN

[EPLAN Electric P8](https://www.eplan-software.com/solutions/eplan-electric-p8/) ist eine CAE-Lösung zur Projektierung, Dokumentation und Verwaltung von elektrotechnischen Automatisierungsprojekten.

[EPLAN API Help](https://www.eplan.help/de-de/Infoportal/Content/api/2023/index.html) - Beschreibung und Beispiele zur Verwendung der EPLAN API.

Beispiele für die EPLAN Electric P8 Automatisierung in der Programmiersprache C# finden Sie hier -
[Suplanus](https://github.com/Suplanus)

### EasyEPLANner

<img src="../user_manual/images/EasyEplannerPreview.png">

Der EasyEPLANner wurde als Add-In zur EPLAN 2.9-Version entwickelt. Das Add-In wird für EPLAN-Projekte verwendet und hilft Ihnen, die Arbeit eines Automatisierungsingenieurs und eines Softwareentwicklers zu automatisieren. Der Softwareentwickler erstellt den Code in der Programmiersprache Lua für das Projekt.

EasyEPLANner hilft bei der Beschreibung von technologischen Objekten (Tank, Kessel usw.), Operationen dieser Objekte, Arbeitsschritten, Betriebsbeschränkungen und vielen anderen Eigenschaften. Schließlich generiert EasyEPLANner LUA-Dateien, die auf einen Kontroller hochgeladen werden. Wir unterstützen die Kontroller folgender Hersteller:

1. [Phoenix Contact - PLCNext](https://github.com/plcnext);
2. [WAGO - PFC200](https://github.com/WAGO).

### Wie man baut
Sie können das Repository mit dem nächsten Befehl klonen:

```bash
git clone --recurse-submodules https://github.com/sav-oss-eplanner/EasyEPLANner.git
```

oder, wenn Sie geklont werden, ohne die Untermodule zu initialisieren und zu aktualisieren, versuchen Sie Folgendes:

```bash
git submodule update --init --recursive
```

Herzlichen Glückwunsch, Sie können jetzt diese Lösung erstellen (_wenn Sie die EPLAN P8 Anwendung haben_).

### Benutzerdokumentation
Die neueste Version der Benutzerdokumentation für das Projekt finden Sie [hier](../user_manual/ReadMe.md).

### Beitrag
Wenn Sie zur Entwicklung unseres Projekts beitragen möchten, lesen Sie vor dem Beginn [wie man es am besten machen kann](../contributing.md).

### Feedback
Kontaktieren Sie uns über Slack:

* Kanal in [Slack](https://slack.com) - EasyEPLANner.slack.com.

### Verhaltensregeln
Wir [verwenden](../CODE_OF_CONDUCT.md)
Standardverhaltensregeln (Kommunikation), die vom GitHub-Dienst bereitgestellt werden.

### Codestil
Wir verwenden C# - und LUA-Programmiersprachen für die Entwicklung. Wir haben [eigene Vereinbarungen](../codestyle.md), die Sie befolgen müssen, um den Code bequem und lesbar zu machen.


### Lizenz
Das Projekt ist unter der Lizenz [MIT](../../LICENSE.txt) lizensiert.
