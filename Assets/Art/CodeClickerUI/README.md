# Code Clicker UI assets

Deze map bevat een samenhangende set 2D-rasterassets voor de Unity-versie van Code Clicker.

## Bestanden

- `background.png` — 16:9 achtergrond voor het volledige scherm.
- `computer_workstation.png` — transparante computer/workstation met decoratieve code.
- `shop_panel.png` — transparant winkelpaneel met vijf lege itemrijen.
- `icon_junior_developer.png` — transparant shop-icoon.
- `icon_ai_assistant.png` — transparant shop-icoon.
- `icon_mechanical_keyboard.png` — transparant shop-icoon.
- `icon_dual_monitors.png` — transparant shop-icoon.
- `icon_coffee_machine.png` — transparant shop-icoon.

## Aanbevolen Unity-importinstellingen

Gebruik voor alle bestanden `Texture Type: Sprite (2D and UI)` en `Sprite Mode: Single`.
Laat bij de transparante assets `Alpha Is Transparency` aan staan. Zet `Mesh Type` op
`Full Rect` wanneer je de afbeeldingen als UI Images gebruikt. De achtergrond kan op een
Canvas Image met `Preserve Aspect` uit; voor de andere assets is `Preserve Aspect` juist handig.

Plaats prijzen, aantallen, namen en knoppen als gewone Unity UI/TextMeshPro-elementen bovenop
het lege winkelpaneel. De code in het computerscherm is decoratief. Voor code die tijdens het
spelen verandert, plaats je een TextMeshPro-element boven het schermgebied.

## Visuele richting

De set is met de ingebouwde image generator gemaakt als gepolijste, handgeschilderde 2D
casual-gamekunst. Alle prompts gebruikten dezelfde donkerblauwe, teal-, cyan-, amber-,
walnoot- en crèmekleurige programmeursstijl. Er is geen bestaande conceptafbeelding als
bewerkbaar bronbestand gebruikt; de onderdelen zijn als nieuwe, onderling consistente assets
gegenereerd.
