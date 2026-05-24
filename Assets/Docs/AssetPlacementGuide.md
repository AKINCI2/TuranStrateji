# Turan Strateji Asset Kurulum Rehberi

Bu proje Warpath tarzinda iki ana gorsel katmanla ilerler:

- `WorldMap`: Hex harita, kaynak noktalar, haritadaki us isareti, sahadaki birlikler.
- `BaseView`: Us ici binalar, kislalar, uretim tesisi, us icinde gorunen egitilmis birlik temsilcileri.

Ayri Unity scene acmayacagiz. Profesyonel ve hizli gecis icin ayni sahnede `GameModeManager` iki root'u acar/kapatir.

## Birlik Modeli Ekleme

Her timin ana datası `Assets/Resources/Data/Units/...` altindaki `TuranUnitData` assetidir.

Yeni bir tim modeli hazirladiginda:

1. FBX/model dosyasini `Assets/Models/Units/<Ulke>/<TimAdi>/` altina koy.
2. Prefab haline getir: `Assets/Prefabs/Units/<Ulke>/<TimAdi>_Unit.prefab`.
3. Prefab root'unda `UnitController` olmali.
4. Piyade ise cocuk objelerde `FormationController` ve asker transformlari bulunmali.
5. Ilgili `TuranUnitData` assetini ac.
6. `World Prefab` alanina bu prefab'i surukle.
7. Kart gorseli icin `Icon` alanina 2D sprite ata.

Kural: `OwnedUnit` sadece envanter/tim kartidir. Haritada ve uste gorunen fiziksel birlik, kislada asker egitildikten sonra olusur.

## Subay Portresi Ekleme

Subay datalari `Assets/Resources/Data/Officers/...` altindadir.

1. Portre PNG/JPG dosyasini `Assets/UI/Officers/Portraits/` altina koy.
2. Import Settings:
   - Texture Type: `Sprite (2D and UI)`
   - Max Size: `1024`
   - Compression: `Normal Quality`
3. Ilgili `OfficerData` assetini ac.
4. `Portrait` alanina sprite'i surukle.

Subay paneli su anda portre yoksa renkli yer tutucu gosterir. Portre atandiginda otomatik gercek portreye gecer.

## Kaynak ve Ikmal Gorselleri

Ikmal cantasindaki kalas, beton, cimento, tugla, kupon, hizlandirma ve sandik kartlari su an renkli kutularla temsil ediliyor.

Profesyonel kurulum icin sonraki adimda bu itemlar `InventoryItemData` yapisina alinacak:

- `displayName`
- `icon`
- `category`
- `amount`
- `description`

Haritadaki kaynak modelleri `WorldResourceNodeManager` tarafindan uretilir. Kalas/beton/cimento modeli hazirlandiginda:

1. Modeli `Assets/Models/Resources/<KaynakAdi>/` altina koy.
2. Prefab'i `Assets/Prefabs/Resources/<KaynakAdi>_Node.prefab` olarak olustur.
3. Prefab root'una collider ekle.
4. Sonraki kod adiminda `WorldResourceNodeManager` icine resource prefab tablosu eklenecek ve bu prefablar kaynak tiplerine baglanacak.

## Bina Modeli Ekleme

Bina datalari `Assets/ScriptableObjects/Buildings/...` altindaki `BuildingData` assetleridir.

Her seviye icin ayri model en temiz yoldur:

- `HQ_Level01.prefab`
- `HQ_Level05.prefab`
- `HQ_Level10.prefab`
- `UretimTesisi_Level01.prefab`
- `UretimTesisi_Level05.prefab`

Model kurali:

- Root scale `1,1,1` kalmali.
- Yan yatma varsa model prefab'i altindaki `Visual` child'inda rotation duzeltilmeli.
- Collider sadece bina oturum alani kadar olmali; buyuk collider baska binaya tiklamayi bozar.
- `BaseBuilding > Data` alanina bina datası bagli kalmali.

