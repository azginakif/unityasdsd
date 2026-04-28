# MOBIL OFL - Oyun Konsepti

## Kisa Tanim
4 kisiye kadar oynanabilen, mobil odakli, first person, online co-op bir dedektiflik oyunu. Oyun bir okulda gecer. Oyuncular birlikte delil toplar, suphelileri sorgular ve dogru olay zincirini ortaya cikarmaya calisir.

## Temel Hedef
Oyuncular tek basina degil ekip olarak dusunmek zorunda kalmali. Bir oyuncunun buldugu bilgi digerinin buldugu delille birlesince anlam kazanmali.

## Oynanis Dongusu
1. Lobiye gir ve 1-4 oyunculuk takim kur.
2. Okul haritasinda farkli bolgelere dagil.
3. Delil, guvenlik kaydi, not, esya ve tanik ifadesi topla.
4. Bulunan bilgileri takim ekraninda birlestir.
5. Kucuk gorevleri cozerek yeni alanlar ac.
6. Suclu, motivasyon ve olay sirasina dair ortak karar ver.
7. Final toplantisinda suclamayi yap ve sonucu gor.

## Oyun Yapisi
- Mekan: Lise kampusu
- Bolumler: Siniflar, ogretmenler odasi, kantin, spor salonu, laboratuvar, kutuphane, mudur odasi, bodrum arsiv
- Mac suresi: 20-30 dakika
- Oyuncu sayisi: 1-4
- Kamera: First person
- Platform: Android / iOS

## Hikaye Cekirdegi
Okulda ciddi bir olay yasanir. Bu olay dogrudan cinayet olmak zorunda degil.

Mobil ve genis kitle icin guvenli senaryo ornekleri:
- Cok onemli sinav sorularinin calinmasi
- Okul kasasindan kritik bir dosyanin kaybolmasi
- Bir ogrencinin kaybolmasi
- Guvenlik sisteminin sabote edilmesi

En iyi baslangic senaryosu:
`Sinav sorulari calindi ve olayin ustu ortulmeye calisiliyor.`

## Oyuncu Rolleri
Roller sert siniflar gibi degil, hafif uzmanlik alanlari gibi calismali.

- Arastirmaci: Delilleri daha hizli inceler.
- Teknik Oyuncu: Kamera kayitlari ve kilitli terminallerle ilgilenir.
- Gozlemci: Cevresel ipuclarini daha kolay fark eder.
- Sosyal Oyuncu: NPC sorgularinda ek diyalog secenekleri acar.

## Co-op Tasarim Mantigi
Co-op sadece beraber dolasmak olmamali. Bilgi parcali dagitilmali.

Ornek:
- Bir oyuncu laboratuvarda kirik telefon bulur.
- Baska oyuncu kutuphanede o telefona ait notu bulur.
- Ucuncu oyuncu guvenlik odasinda saat bilgisini bulur.
- Tum bilgiler birlesince dogru supheli ortaya cikar.

## Ana Sistemler

### 1. Delil Toplama
- Yaklas ve etkilesime gir
- Delil takim envanterine dussun
- Her delilin kategori etiketi olsun:
  - Belge
  - Dijital kayit
  - Fiziksel iz
  - Tanik ifadesi

### 2. Ortak Delil Panosu
- Tum takimin gordugu bir kanit tahtasi
- Oyuncular ipuclarini birbirine baglayabilsin
- Dogru baglantilar yeni cikarim metinleri acsin

### 3. NPC Sorgu Sistemi
- Ogrenciler, ogretmenler, guvenlik gorevlisi, hademe
- NPC ayni soruya herkese ayni cevabi vermesin
- Bazi cevaplar bulunan delile gore acilsin

### 4. Olay Yeniden Kurma
- Finale yakin takim `su kisi, su saatte, su sebeple yapti` gibi secim yapar
- Sistem toplanan kanitlara gore basari puani verir

### 5. Tehdit ve Baski Mekanigi
- Surekli korku yerine zaman baskisi kullanilabilir
- Mudurun devriye gezmesi
- Bazi odalarin belirli sure sonra kapanmasi
- Yanlis alarm verilince NPC davranislarinin degismesi

## Mobil Icin Tasarim Kararlari
- Harita tek bina veya tek kampus blogu olmali
- Kisa mac yapisi kullanilmali
- Ayni anda cok fazla NPC olmamali
- Dokunmatik kontrol sade tutulmali
- UI kalabalik olmamali
- Isik, golge ve post-process mobil icin hafif tutulmali

## Teknik Yon
Ilk teknik hedef:
- 4 oyunculu oda sistemi
- Basit first person controller
- Etkilesimli delil objeleri
- Senkronize takim delil listesi

Unity tarafinda mantikli secenekler:
- `Netcode for GameObjects + Unity Relay/Lobby`
- `Photon Fusion`

Hizli prototip icin Photon Fusion daha pratik olabilir.
Unity ekosisteminde duzenli ilerlemek icin NGO + Relay/Lobby daha uygun olabilir.

## MVP Kapsami
Ilk surum kucuk tutulmali.

### MVP Harita
- Giris koridoru
- 2 sinif
- Ogretmenler odasi
- Kutuphane
- Guvenlik odasi

### MVP Oyun Akisi
- Oyuncular spawn olur
- 6-8 delil toplanir
- 3 NPC ile konusulur
- Bir guvenlik kaydi acilir
- Final suclama ekrani gelir

### MVP Basari Kosulu
- Dogru supheli
- Dogru motivasyon
- Dogru zaman cizelgesi

## Ornek Ilk Bolum
Olay:
Sinav sorulari okul icinden biri tarafindan calindi.

Supheliler:
- Basarili ama baski altindaki ogrenci
- Borcu olan kantin calisani
- Sisteme erisimi olan bilisim kulubu ogrencisi
- Bir ogretmen yardimcisi

Gercek cozum:
Tek bir suclu yerine iki kisinin ortak plani olabilir. Bu, takim tartismasini guclendirir.

## Oyunun Guclu Tarafi
- Korku degil gerilim
- Aksiyon degil arastirma
- Tek basina cozmeye degil takim ici bilgi paylasimina odaklanma

## Uretim Sirasi
1. Mobil first person kontrol
2. Online oda kurma ve baglanma
3. Ortak etkilesim sistemi
4. Delil veri yapisi
5. Delil panosu
6. NPC sorgu sistemi
7. Tek bolum prototipi

## Net Karar
Bu fikir icin en dogru yon:
`4 kisilik mobil co-op okul dedektiflik oyunu`

Alt tur:
`Narrative co-op mystery investigation`
