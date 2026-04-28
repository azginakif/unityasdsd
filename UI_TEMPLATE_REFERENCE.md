# UI Template Reference

Bu projedeki yeni HUD gorsel dili, asagidaki hazir arayuz referansina gore secildi:

- Kenney UI Pack
  - Kaynak: https://kenney.nl/assets/ui-pack
  - Lisans: CC0

## Bu Referanstan Alinan Yonler
- Kalin ve okunur mobil tipografi
- Buyuk dokunmatik buton mantigi
- Kart / panel bolme yapisi
- Net accent renkleri ile durum ayrimi
- Hedef, delil ve co-op gibi bilgiler icin ayri bilgi kartlari

## Projede Uygulanan Karsiligi
- `ModernGuiTheme.cs` ile koyu panel + yesil/turuncu accent sistemi
- `CaseNotebookHud`, `CaseStatusHud`, `OnlineSessionHud`, `FocusReticleHud` uzerinde ortak stil
- Mobilde daha rahat tiklanan butonlar ve bilgi kartlari

Not:
Bu ortamda asset dosyalari dogrudan indirilemedigi icin hazir paketin PNG/Sprite dosyalari import edilmedi.
Onun yerine ayni gorsel dil kod tarafinda uretilip projeye uygulandi.
