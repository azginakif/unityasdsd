UI yonu icin arastirma notlari

1. Kenney UI Pack
- Link: https://kenney.nl/assets/ui-pack
- Neden mantikli:
  - CC0, yani referans gorsel dili icin guvenli.
  - Temiz panel, button, badge ve inventory dili var.
  - Mobil prototipte okunakli ve oyun hissi veren sade bir yon sunuyor.

2. Unity UI Extensions
- Link: https://unity-ui-extensions.github.io/
- Neden mantikli:
  - Acik kaynak.
  - UGUI uzerine ek kontrol ve layout bilesenleri getiriyor.
  - Ozellikle carousel, segmented control, nicer scroll ve ek efektlerde faydali olabilir.

Bu turda ne yapildi
- Dis kutuphane importu zorunlu hale getirilmedi; once mevcut HUD sadeleştirildi.
- Sebep:
  - Proje zaten runtime uGUI ile yazili.
  - Package importunu buradan resolve edip test edemiyorum.
  - Once bilgi mimarisini toparlamak, sonra harici kutuphane eklemek daha guvenli.

Sonraki mantikli adim
- Unity icinde package resolve denenerek `Unity UI Extensions` eklenebilir.
- Gorsel asset tarafinda Kenney UI Pack referans alinarak buton, badge ve panel sprite seti elde eklenebilir.
