# GitHub Yukleme Rehberi

Bu ortamda `git` kurulu olmadigi ve dogrudan GitHub'a baglanamadigim icin push islemini burada tamamlayamadim.

## 1. Git kur
Windows icin Git kur:
- https://git-scm.com/download/win

## 2. GitHub'da repo olustur
GitHub uzerinde yeni bir bos repo ac.
Ornek isim:
- `mobil-ofl`

## 3. Bu klasorde terminal ac
Proje klasoru:
- `C:\Users\OF FEN LİSESİ-10\Desktop\Unity File Hüseyin Yeşilyurt , Ali Küçük\MOBİL OFL`

## 4. Su komutlari calistir
```powershell
git init
git add .
git commit -m "Initial Unity prototype upload"
git branch -M main
git remote add origin https://github.com/KULLANICI_ADIN/mobil-ofl.git
git push -u origin main
```

## 5. GitHub Desktop alternatifi
Komut satiri istemezsen GitHub Desktop ile de ayni klasoru publish edebilirsin.
- https://desktop.github.com/

## Dikkat
`.gitignore` hazirlandigi icin su klasorler yuklenmeyecek:
- `Library`
- `Temp`
- `Logs`
- `UserSettings`