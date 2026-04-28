# MVP Kurulum Notlari

## 0. Gereksinimler
- Unity surumu: `6000.4.3f1`
- Render pipeline: `URP`
- Aktif paketler:
- `com.unity.inputsystem`
- `com.unity.ugui`
- `com.unity.render-pipelines.universal`
- `com.unity.ai.navigation`
- `com.unity.multiplayer.center`
- `com.unity.netcode.gameobjects`
- `com.unity.transport`
- `com.unity.services.authentication`
- `com.unity.services.relay` `su anki gecis yolu`
- `com.unity.services.multiplayer` `Unity 6 icin onerilen yeni yon`

## 1. Onerilen Tek Tik Kurulum
Unity icinde ust menuden:
- `Mobil OFL > Setup > Auto Setup Investigation Scene`

Bu islem otomatik olarak sunlari yapar:
- Onerilen mobil proje ayarlarini uygular
- Aktif sahneyi Build Settings listesine ekler
- `Assets/Data/Cases/ExamTheftCase.asset` olusturur veya gunceller
- `Player` root objesini kurar
- `CharacterController` ekler
- `Main Camera` kurar
- `PrototypeFirstPersonController` ekler
- `PlayerInteractionController` ekler
- `CaseSessionManager` ekler
- `NetworkManager`, `UnityTransport`, `RelayNetworkBootstrap` ekler
- `NetworkCaseState` scene objesini kurar
- `Assets/Prefabs/Generated/NetworkPlayer.prefab` olusturur veya gunceller
- `CaseStatusHUD` ekler
- `CaseChecklistHUD` ekler
- `CaseNotebookHUD` ekler
- `CaseResultHUD` ekler
- `FocusReticleHUD` ekler
- `WorldMarkerHUD` ekler
- SchoolMinimapHUD ekler
- LocationBannerHUD ekler
- SchoolExplorationTracker ekler
- InvestigationWaypointHUD ekler
- OnlineSessionHUD ekler
- MainMenuHUD ekler
- `MobileControlsCanvas` ve `EventSystem` kurar
- Basit okul prototip blogunu sahneye yerlestirir
- `Vaka Masasi` kosesini ve notebook acan fiziksel etkilesim masasini kurar
- Koridor banklari, dolaplar, panolar ve kutuphane raflari gibi ek cevre detaylarini kurar
- Atmosfer isiklari ve sis ayarini uygular
- 4 adet ornek delil objesi ekler
- Delil pulse ve glow efektlerini baglar
- 5 adet ornek NPC olusturur
- Eski `DebugHUD` objesini pasif hale getirir

## 2. Ayrica Uygulanabilen Setup Menusu
Sadece proje ayarlarini guncellemek istersen:
- `Mobil OFL > Setup > Apply Recommended Mobile Project Settings`

Bu menu su ayarlari uygular:
- `Company Name`: `Mobil OFL`
- `Product Name`: `MOBIL OFL`
- `Bundle Version`: `0.2.0`
- `Android Application Identifier`: `com.mobilofl.prototype`
- `iOS Application Identifier`: `com.mobilofl.prototype`
- `Default Orientation`: `Landscape Left`
- `Run In Background`: acik

## 3. Manuel Kurulum

### 3.1 Vaka Asset'i Olustur
Unity icinde:
- `Assets` uzerinde sag tik
- `Create > Mobil OFL > Case Definition`
- Adini `ExamTheftCase` yap

Asset icinde su alanlari doldur:
- `Case Title`: `Sinav Sorulari Calindi`
- `Evidence Items`: en az 3 delil
- `Suspects`: en az 2 supheli
- `Culprit Suspect Id`: gercek suclunun id'si

### 3.2 Session Manager Kur
- Sahneye bos bir obje ekle: `CaseSessionManager`
- Uzerine `CaseSessionManager` scriptini ekle
- `Active Case` alanina olusturdugun `ExamTheftCase` asset'ini ver

### 3.3 Oyuncu Kur
- Sahneye `Player` objesi ekle
- Uzerine `CharacterController` ekle
- Uzerine `PrototypeFirstPersonController` ekle
- Uzerine `PlayerInteractionController` ekle
- `Player Camera` alanina aktif kamerayi ver

### 3.4 HUD Kur
- Sahneye bos bir obje ekle: `CaseStatusHUD`
- Uzerine `CaseStatusHud` ekle
- `Player Interaction` alanina oyuncudaki `PlayerInteractionController` scriptini ver
- Sahneye bos bir obje ekle: `CaseChecklistHUD`
- Uzerine `CaseChecklistHud` ekle
- Sahneye bos bir obje ekle: `CaseNotebookHUD`
- Uzerine `CaseNotebookHud` ekle
- Sahneye bos bir obje ekle: `CaseResultHUD`
- Uzerine `CaseResultHud` ekle
- Sahneye bos bir obje ekle: `FocusReticleHUD`
- Uzerine `FocusReticleHud` ekle
- `Player Interaction` referansini bagla
- Sahneye bos bir obje ekle: `WorldMarkerHUD`
- Uzerine `WorldMarkerHud` ekle
- `Target Camera` alanina aktif kamerayi bagla
- Sahneye bos bir obje ekle: `SchoolMinimapHUD`
- Uzerine `SchoolMinimapHud` ekle
- `Player Target` alanina oyuncu root transformunu bagla
- Sahneye bos bir obje ekle: OnlineSessionHUD

- Uzerine OnlineSessionHud ekle
- RelayNetworkBootstrap referansini bagla
- Sahneye bos bir obje ekle: LocationBannerHUD

- Uzerine LocationBannerHud ekle
- Sahneye bos bir obje ekle: MainMenuHUD

- Uzerine MainMenuHud ekle
- RelayNetworkBootstrap referansini bagla

### 3.5 Mobil Kontroller
- Sahneye bir `Canvas` ekle
- `Screen Space Overlay` kullan
- Sol alt icin joystick
- Sag taraf icin bakis alani
- `KOS`, `ZIPLA`, `AL`, `DOSYA` butonlarini ekle
- Sahneye `EventSystem` ekle
- `InputSystemUIInputModule` kullandigindan emin ol

### 3.6 Delil Objeleri Kur
Her delil icin:
- Sahneye bir obje koy
- Collider ekle
- `EvidenceInteractable` ekle
- `Case Definition` alanina ayni vaka asset'ini ver
- `Evidence Id` alanina asset icindeki delilin `id` degerini yaz

### 3.7 NPC Kur
Her NPC icin:
- Sahneye collider'i olan bir obje koy
- `NpcInteractable` ekle
- `Case Definition` alanina aktif vaka asset'ini ver
- `Required Evidence Id` alanina gerekiyorsa acici delili yaz
- `Witness Evidence Id` alanina gerekiyorsa konusma ile acilan delili yaz

### 3.8 Online Altyapi Kur
- Sahneye `NetworkManager` objesi ekle
- `NetworkManager`, `UnityTransport`, `RelayNetworkBootstrap` ekle
- `NetworkConfig > Player Prefab` alanina `NetworkPlayer.prefab` ver
- Sahneye `NetworkCaseState` objesi ekle
- `NetworkObject` ve `NetworkCaseState` ekle
- `Case Definition` alanina aktif vaka asset'ini bagla

## 4. Oyun Ici Test Listesi
- Oyun acilinca ust solda vaka bilgisi gorunmeli
- Sol altta gorev kontrol listesi gorunmeli
- Sag tarafta mini harita gorunmeli
- Delile bakinca reticle rengi degismeli
- Delile bakinca prompt gorunmeli
- Dunya uzerinde delil ve NPC marker'lari gorunmeli
- Klavyede `E`, mobilde `AL` ile delil toplanabilmeli
- `Tab` veya mobilde `DOSYA` ile vaka dosyasi acilabilmeli
- Vaka dosyasinda `Genel Durum`, `Deliller`, `Sorgular`, `Notlar`, `Supheliler` sekmeleri gorunmeli`r`n- Genel Durum sekmesinde `Dosya Analizi` metni gorunmeli`r`n- Uzun sure ilerleme olmazsa otomatik dosya yardimi mesaji gelmeli
- Online HUD'da `Offline Host`, `Online Host`, `Oturuma Katil` butonlari gorunmeli
- Online HUD join code'u kopyalayip yapistirabilmeli
- Online HUD oyuncu hazirlik durumlarini gostermeli
- Oyuna giriste ana menu acik gelmeli
- Ana menude oyuncu adi, gorev ozeti ve oturum kartlari gorunmeli
- Koridordan baska odaya gecince lokasyon banneri cikmali
- Alt orta kisimda sonraki mantikli hedefi gosteren yonlendirme waypoint paneli cikmali
- Notebook ve ust HUD icinde kesif edilen bolge sayisi gorunmeli
- `Vaka Masasi` ile etkilesince notebook acilmali
- Notebook icindeki `Notlar` sekmesi takim notlarini gostermeli ve yeni not ekleyebilmeli
- Vaka dosyasi acikken hareket ve etkilesim durmali
- Toplanan deliller vaka dosyasinda acilmali
- NPC ile konusunca yeni mesaj gelmeli
- NPC sorgulari `Sorgular` sekmesinde kayda dusmeli
- Gerekli deliller toplaninca supheli suclanabilmeli
- Dogru supheli secilirse sonuc paneli acilmali
- Sonuc panelinde sure, kritik delil ve sorgu ozeti gorunmeli
- Yanlis supheli secilirse kisa uyari gosterilmeli

## 5. Build Kontrolu
- `File > Build Profiles` icinde aktif sahnenin listede oldugunu kontrol et
- Android build icin `Application Identifier` degerinin template adindan ciktigini kontrol et
- Input System acik degilse proje acilisinda Unity'nin yeniden baslatma istemini onayla

## 6. Sonraki Mantikli Adimlar
- Netcode/Relay veya Photon ile 4 kisilik oda sistemi
- Gercek okul modeli veya moduler prefab seti
- Gorev ilerleme ve bolum sonu akisi
- Delil panosu ve takim ici ortak not sistemi



## 7. Unity 6 notu
- Web arastirmasina gore Unity 6 icin uzun vadeli dogru yon `com.unity.services.multiplayer` tabanli MPS/Session akisi.
- Bu proje su an derli toplu bir prototip olarak `Authentication + Relay + NGO + UTP` yolunu koruyor.
- Gecis notlari icin [UNITY_MULTIPLAYER_RESEARCH_TR.md](UNITY_MULTIPLAYER_RESEARCH_TR.md) dosyasina bak.




