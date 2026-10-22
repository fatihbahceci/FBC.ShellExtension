---
title: Wiki.js'e gönderme
description: Typora'da yazılmış bir markdown dosyasını resimleriyle birlikte Explorer'ın sağ tık menüsünden bir Wiki.js 2 sayfasına aktarmak.
published: true
tags: fbctools, wiki.js, typora, markdown
editor: markdown
---

# Wiki.js'e gönderme

**Send to Wiki.js…**, FBCTools'un yerleşik araçlarından biridir. Yerelde yazdığınız bir markdown dosyasını (Typora'da ya da başka bir düzenleyicide) bağlantı verdiği resimlerle birlikte alır ve wiki'nin API'si üzerinden bir Wiki.js sayfasına koyar. Sayfayı adresiyle seçersiniz; resimler sayfanın yoluyla aynı yoldaki bir asset klasörüne yüklenir, sayfadaki bağlantılar onları gösterecek biçimde yeniden yazılır.

> Wiki **Wiki.js 2.5** olmalı; araç bu sürüme göre yazıldı. Daha eski 2.x sürümlerinde kullandığı API'nin bazı parçaları yok, Wiki.js 3'ün API'si ise başka. İkisi de desteklenmiyor.
{.is-info}

## Nasıl çalışır

1. Explorer'da bir `.md` dosyasına sağ tıklayıp **FBCTools › Text files › Send to Wiki.js…**'yi seçin.
2. Sayfanın adresini tarayıcının gösterdiği gibi yapıştırın, örneğin `https://wiki.example.com/en/internal/ekip/baslangic`. Henüz olmayan bir sayfa oluşturulur; aynı dosyayı bir dahaki gönderişte son adresi hazır gelir.
3. Bir wiki için ilk seferde FBCTools bir API anahtarı ya da kendi belgenizi (aşağıda) sorar ve Windows hesabınıza özel şifreleyerek saklar. Saklananı FBCTools Editor'de *Tools › Wiki.js key or token…* ile değiştirebilir ya da silebilirsiniz.
4. Sayfa zaten varsa içeriğini değiştirmeden önce sorar. Görsel ya da HTML editörüyle yazılmış bir sayfa değiştirilmez; önce wiki'de Markdown'a çevirin.
5. Bir pencere her adımı gösterir: bulunan resimler, asset klasörü, her yükleme, oluşturulan ya da güncellenen sayfa ve sonunda sayfanın adresi.

Dosyadaki resim bağlantıları şu biçimlerden herhangi biriyle yazılmış olabilir:

| Dosyada | Ne olur |
| --- | --- |
| `![alt](not.assets/resim.png)` | Yüklenir; bağlantı wiki'yi gösterir |
| `![alt](not.assets/benim%20resmim.png "Başlık")` | Dosyayı bulmak için `%20` ve benzerleri çözülür; başlık kalır |
| `<img src="not.assets/resim.png" style="zoom:50%;">` (Typora, boyut değişince) | Yüklenir; yalnız `src` değişir, öteki öznitelikler kalır |
| `![alt](C:\Resimler\resim.png)` | Tam yollar da olur |
| `![alt][logo]` ve bir `[logo]: not.assets/logo.png` satırı | Yüklenir; `[logo]:` satırı wiki'yi gösterir |
| `![alt](https://example.com/logo.png)`, `data:` | Dokunulmaz |
| Diskte olmayan bir dosya | Eksik olarak yazılır; bağlantısı olduğu gibi kalır, sayfa yine kaydedilir |

Birkaç yerden bağlanan bir resim bir kez yüklenir. Çitli kod bloklarındaki (```` ``` ```` ya da `~~~`) ve kod parçalarındaki resimlere dokunulmaz. Çitsiz, dört boşluk girintili bir blok kod olarak tanınmaz; içindeki resimler ötekiler gibi yüklenir.

## Yol kuralı

Resimler, yolu sayfanın **locale'siz** yolu olan bir asset klasörüne gider:

| | Adres |
| --- | --- |
| Sayfa | `https://wiki.example.com/en/internal/third-party/about-markdown` |
| Resim | `https://wiki.example.com/internal/third-party/about-markdown/image-20210622223543110.png` |

Sayfa yolunun her parçası, wiki'nin sakladığı gibi küçük harfle, iç içe bir asset klasörü olur (`internal` › `third-party` › `about-markdown`); olmayanlar oluşturulur. Bağlantılar varsayılan olarak sitenin kökünden yazılır (`/internal/third-party/about-markdown/resim.png`), böylece wiki hangi adla açılırsa açılsın çalışır; tam adres için aşağıda `linkStyle`'a bakın.

Wiki.js yüklenen dosyaların adını değiştirir (küçük harf, boşluk yerine `_`). FBCTools her resmi zaten bu biçimde, düz ASCII bir adla yükler (`Ekran Görüntüsü 1.PNG` → `ekran_goruntusu_1.png`), sonra klasörün listesini okur ve wiki'nin gerçekten sakladığı ada bağlantı verir.

Bir yükleme aynı adlı dosyanın yerine geçer ve wiki dosyaların geçmişini tutmaz. Bu yüzden **yeni** bir sayfa klasöründe zaten olan hiçbir şeyin yerine geçmez: adı orada alınmış bir resim `ad-2.png` olarak yüklenir. **Var olan** bir sayfayı yeniden göndermek ise daha önce yüklediği resimlerin yerine geçer; pencere bunu her biri için yazar.

## Wiki'de, ilk aktarımdan önce

Bunu yönetici yetkisi olan biri bir kez yapar.

1. **API'yi açın.** *Administration › API Access*: **Enable API**'yi açın.
2. **Anahtar için bir grup oluşturun.** *Administration › Groups*, yeni bir grup, örneğin *API Import*:
   - **Permissions:** `read:pages`, `write:pages`, `read:assets`, `write:assets`.
   - **Page rules:** aktaracağınız yollar için `read:pages`, `write:pages`, `read:assets` ve `write:assets`'e izin verin, örneğin *Path starts with* `internal`, **bütün locale'ler** için. Yolu **sonunda bölü olmadan** yazın: wiki `internal` asset klasörünü kendi yoluyla denetler, `internal/` onu kapsamaz (kural böylece `internal-eski/…`'yi de kapsar; bedeli bu). Yükleme, asset'in locale taşımayan yoluna göre denetlenir; tek bir locale'e sınırlı kural onu kapsamaz.
3. **Anahtarı oluşturun.** *Administration › API Access › New API Key*: bir ad, bir **son kullanma süresi** (30 günden 3 yıla; kısa olanı tercih edin) ve *Full Access* yerine yukarıdaki grup. Anahtarı hemen kopyalayın: wiki onu yalnız bu bir kez gösterir.
4. **Yükleme sınırı.** *Administration › Security › Uploads* daha fazlasına izin vermedikçe Wiki.js 5 MB'tan büyük dosyaları reddeder.

> Anahtara parola gibi davranın: elinde olan herkes grubun yetkileriyle wiki'ye yazabilir. İşi bitince *API Access*'ten iptal edin.
{.is-warning}

## Kendi hesabınızla: tarayıcıdaki belge

Wiki'ye şirket hesabınızla (Azure AD gibi) giriyorsanız API anahtarı yerine kendi oturum belgenizi kullanabilirsiniz. O zaman:

- kimseyle anahtar paylaşmanız gerekmez;
- FBCTools wiki'de sizin yetkilerinizle çalışır;
- sayfa geçmişinde sizin adınız görünür (anahtarla yönetici görünür);
- wiki'de *API Access* kapalı olsa da çalışır.

Belgeyi almak için:

1. Wiki'yi tarayıcıda (Edge, Chrome) açıp her zamanki gibi giriş yapın.
2. **F12**'ye basın. *Application* sekmesinde *Storage › Cookies* altında wiki'nin adresini seçin.
3. **`jwt`** satırının *Value* değerini kopyalayın.
4. FBCTools anahtar sorduğunda bunu yapıştırın. Bir anahtar zaten saklıysa FBCTools Editor'de *Tools › Wiki.js key or token…*'ı açın, wiki'nin adresini seçin, belgeyi yapıştırıp **Check and save**'e basın.

Bilmeniz gerekenler:

- **Belge bir süre sonra çalışmaz.** Wiki.js'in varsayılan ayarlarıyla yaklaşık 14 gün; tarayıcı wiki'yi kullandıkça kendi belgesini yeniler, taze kopyalanan en uzun dayanır. Süresi dolunca FBCTools "The wiki did not accept the API key or token" der ve yeniden sorar: tarayıcıdan taze bir `jwt` kopyalayın. Süreyi yönetici *Administration › Security*'deki *Token Expiration* ve *Token Renewal Period* ile değiştirir.
- **Belgeye parola gibi davranın.** Wiki'de tarayıcıda yapabildiğiniz her şeyi yapabilir. Başkasına verirseniz o kişi sizin adınızla ve yetkinizle çalışır.
- Tarayıcıda wiki'den çıkış yapmak saklanan belgeyi geçersiz kılmaz; wiki hesabı kapatılınca ya da süresi dolunca çalışmaz.

## Ayarlar

FBCTools aracın ayarlarını `%APPDATA%\FBCTools\wiki-import.json`'da tutar. Dosya ilk aktarımda oluşur; yalnız aşağıdaki seçenekler için düzenlemeniz gerekir.

```json
{
  "linkStyle": "path",
  "removeFirstHeading": true,
  "sites": [
    { "baseUrl": "https://wiki.example.com", "apiKey": "<Windows'un şifrelediği hâli, anahtarın kendisi değil>" }
  ],
  "targets": [
    { "file": "C:\\Belgeler\\baslangic.md", "url": "https://wiki.example.com/en/internal/ekip/baslangic" }
  ]
}
```

| Ayar | Anlamı | Varsayılan |
| --- | --- | --- |
| `linkStyle` | `path`: bağlantılar sitenin kökünden (`/internal/sayfa/resim.png`). `fullUrl`: önünde wiki'nin adresiyle | `path` |
| `removeFirstHeading` | Wiki.js sayfa başlığını sayfanın üstünde gösterir; dosyanın açıldığı, genellikle aynı başlığı taşıyan birinci düzey başlık gönderilmez | `true` |
| `sites` | Her wiki için bir kayıt; API anahtarı ya da belge Windows hesabınıza özel şifrelenmiş (DPAPI). FBCTools Editor'de *Tools › Wiki.js key or token…* ile değiştirilir ya da silinir | — |
| `targets` | Her dosyanın en son gittiği adres | — |

**Ortam değişkeni.** `FBC_WIKI_APIKEY` tanımlıysa değeri her wiki için anahtar olarak kullanılır ve hiçbir şey saklanmaz.

**Başlık.** Sayfanın başlığı, dosyanın YAML ön bilgisindeki (front matter) `title`'dır; yoksa dosyanın açıldığı birinci düzey başlık (`# Başlık` ya da altı `===` ile çizili bir satır), o da yoksa dosyanın adı. Daha aşağıdaki bir birinci düzey başlık metnin parçasıdır. Ön bilgi bloğu wiki'ye gönderilmez.

**Sayfayı güncellemek.** İçerik ve başlık dosyadan gelir; adres başka bir büyük/küçük harfle yazılmış olsa da sayfa kendi yolunda kalır. Sayfanın açıklaması, etiketleri ve yayın durumu wiki'deki gibi kalır; CSS ve JavaScript'i de (*Full Access* bir anahtar onları okuyup korur, yukarıdaki grup onlara dokunamaz; kendi belgenizle gruplarınız belirler: `manage:pages` ya da `delete:pages` olmadan `write:styles` ya da `write:scripts` onları boşaltır). **Yayın takvimi** (başlangıç ve bitiş tarihi) ise silinir: Wiki.js 2'nin API'si onu okuyamadığı için koruyamaz. Yeni bir sayfa yayında, açıklamasız ve etiketsiz oluşturulur.

**Öğe nerede görünür.** Varsayılan menü onu *Text files* altında tek bir `.md` dosyası için sunar. FBCTools Editor'de herhangi bir komut onu çalıştırabilir: *Kind* olarak **Send to Wiki.js**'yi seçin.

## Aktarımdan sonra: kontrol listesi

- [ ] Sayfayı açıp bütün resimlerin göründüğüne bakın. *Assets*'te klasör yapısı sayfanın yoluyla aynı.
- [ ] **Açıklamayı** ve **etiketleri** doldurun; yayın durumunu kontrol edin, sayfanın bir yayın takvimi varsa yeniden girin.
- [ ] Typora'ya özgü sözdizimine bakın (aşağıda); gereken render modüllerini *Administration › Rendering*'den açın.
- [ ] Wiki statik bir gezinme menüsü kullanıyorsa sayfayı ona ekleyin.
- [ ] Sayfa sayfa ağacında görünmüyorsa *Administration › Utilities › Content › Rebuild Page Tree*.
- [ ] Git deposu kullanılıyorsa bir sonraki eşitlemeden sonra (varsayılan 5 dakikada bir, ya da *Force Sync*) sayfanın ve resimlerinin depoda olduğuna bakın.
- [ ] Anahtar yalnız bu iş için oluşturulduysa *API Access*'ten iptal edin.

## Wiki.js 2'de Typora sözdizimi

| Sözdizimi | Wiki.js 2'de |
| --- | --- |
| `==vurgu==` | İşlenmez, yazıldığı gibi görünür. `<mark>…</mark>` kullanın |
| `[TOC]` | Desteklenmez. Wiki.js içindekiler tablosunu başlıklardan kendisi kurar |
| `$…$`, `$$…$$` | KaTeX, varsayılan açık (MathJax kapalı) |
| ` ```mermaid ` | İşlenir (eski bir Mermaid sürümü: yeni diyagram türleri çalışmayabilir) |
| `x^2^`, `H~2~O` (üst ve alt simge; içinde boşluk olmadan) | İşlenir |
| Dipnotlar `[^1]`, görev listeleri, emoji | İşlenir |
| YAML ön bilgisi | Gönderilmez; `title`'ı sayfa başlığı olur |

## Sınırlar ve sorun giderme

| Ne görürsünüz | Neden | Ne yapmalı |
| --- | --- | --- |
| "API Key is invalid or was revoked" | Anahtar wiki'de iptal edilmiş ya da silinmiş | FBCTools anahtarı yeniden sorar; yenisini oluşturun |
| "API is disabled" | API kapalı | *Administration › API Access › Enable API* |
| Kimlik doğrulama mesajıyla "The wiki did not accept the API key or token" | Anahtarın ya da belgenin süresi dolmuş, yanlış yazılmış ya da başka bir wiki'nin: Wiki.js istekleri o zaman misafir sayar | FBCTools yeniden sorar. Belgeyse tarayıcıdan taze bir `jwt` kopyalayın; anahtarsa kontrol edin ya da yenisini oluşturun |
| "The asset folder "…" is there, but the API key or token does not see it" | Sayfa kuralı bölüyle bitiyor (`internal/`) | Kuralı bölü olmadan yazın (yukarıda) |
| "… was not uploaded: You are not authorized to upload files" | Grupta `write:assets` yok, ya da yolu bütün locale'lerde kapsayan bir sayfa kuralı yok (belgeyle: sizin gruplarınız) | Grubu düzeltin (yukarıda) |
| Büyük bir resim için "… was not uploaded" | Yükleme sınırından büyük (varsayılan 5 MB) | *Administration › Security › Uploads*'tan artırın ya da resmi küçültün |
| "… is not in the asset folder after its upload" | Wiki "ok" dedi ama dosyayı tutmadı | Wiki'nin log'una bakın |
| "The wiki refused: … (code 6005)" | Sayfa yolunda nokta, boşluk ya da ters bölü var | Başka bir yol seçin |
| "A page path cannot start with …" | `login`, `img`, `js` gibi adlar wiki'nin kendisinin; iki harfli bir ilk klasör dil sayılır | Başka bir ilk klasör seçin |
| "No Wiki.js 2 API answers at …" | Adres yanlış, ya da Wiki.js 3 | Adresi kontrol edin |
| "No answer came from the wiki at … (HTTP 504 …)" | Wiki'nin önündeki sunucu (nginx, IIS gibi bir proxy) beklemeyi bıraktı. Wiki.js sayfayı önce kaydeder, cevabı ise sayfayı işleyip ağaca, aramaya ve depolamaya ekledikten sonra verir | Sayfa yazılırken olduysa FBCTools sayfayı wiki'den geri okur; kaydedildiyse "but the page is saved" der ve aktarım başarılı sayılır. Sık oluyorsa proxy'nin bekleme süresini artırın (nginx: `proxy_read_timeout`) |
| "… sends the request on to …" | Adres yönlendiriyor, genellikle `http://`'den `https://`'e | Söylediği adresi kullanın |
| "The page at … was written with another editor" | Sayfa görsel ya da HTML editörünü kullanıyor | Wiki'de Markdown'a çevirin ya da başka bir adres seçin |
| Sayfa ağaçta yok | | *Administration › Utilities › Rebuild Page Tree* |

- Wiki.js 2, API anahtarıyla yapılan değişikliği wiki'nin ilk kullanıcısına (1 numara, kurulumda oluşturulan yönetici) yazar: geçmişte yazar olarak o hesap görünür. Kendi belgenizle yapılan değişiklik sizin adınıza yazılır.
- Wiki.js 2'de özel (private) sayfa yok; her sayfa wiki'nin kendi yetkileri içinde herkese açık oluşturulur.
- Sayfanın içeriği tek istekte gider; wiki en çok 5 MB kadar metin kabul eder (`bodyParserLimit`).
