# Unity FPS Zombi Oyunu - Script Belgeleri

Bu proje Unity kullanarak geliştirilmiş bir FPS zombi oyunudur. Aşağıda projedeki ana scriptlerin detaylı açıklamaları bulunmaktadır.

---

## 📁 Dosyalar

| Dosya | Açıklama |
|-------|----------|
| `FPSController.cs` | Oyuncu hareket ve kamera kontrol sistemi |
| `ZombieAI.cs` | Zombi yapay zeka ve davranış sistemi |

---

## 🎮 FPSController.cs

Birinci şahıs oyuncu kontrolcüsü. Unity'nin yeni **Input System** paketini kullanır.

### Özellikler

| Özellik | Açıklama |
|---------|----------|
| **Hareket** | WASD tuşları ile yürüme, koşma ve eğilme |
| **Kamera** | Fare ile 360° bakış, Y ekseni ters çevirme desteği |
| **Zıplama** | Space tuşu ile zıplama, yerçekimi hesaplaması |
| **Eğilme** | Ctrl/C tuşları ile toggle eğilme |
| **Headbob** | Yürürken/koşarken kamera sallanma efekti |
| **Ayak Sesleri** | Hareket sırasında rastgele ayak sesi efektleri |

### Inspector Ayarları

```
=== KAMERA AYARLARI ===
- playerCamera: Ana oyuncu kamerası

=== HAREKET AYARLARI ===
- walkSpeed: 5 (Yürüme hızı)
- sprintSpeed: 9 (Koşma hızı)
- crouchSpeed: 2.5 (Eğilme hızı)
- jumpForce: 8 (Zıplama kuvveti)
- gravity: 20 (Yerçekimi)

=== FARE AYARLARI ===
- mouseSensitivity: 0.15 (Fare hassasiyeti)
- maxLookAngle: 85 (Maksimum bakış açısı)
- invertY: false (Y ekseni ters çevirme)

=== HEADBOB AYARLARI ===
- enableHeadbob: true
- walkBobSpeed/Amount: Yürüme sallanması
- sprintBobSpeed/Amount: Koşma sallanması

=== AYAK SESİ AYARLARI ===
- footstepSounds[]: Ayak sesi klipleri
- jumpSound: Zıplama sesi
- landSound: İniş sesi
```

### Gereksinimler
- `CharacterController` component
- `AudioSource` component
- Unity Input System paketi

### Kontroller
| Tuş | Aksiyon |
|-----|---------|
| W/A/S/D | Hareket |
| Fare | Bakış |
| Space | Zıpla |
| Left Shift | Koş |
| Left Ctrl / C | Eğil (Toggle) |

---

## 🧟 ZombieAI.cs

Zombi yapay zeka sistemi. NavMesh tabanlı yol bulma ve katmanlı algılama alanları kullanır.

### Durum Makinesi

```
┌─────────────────────────────────────────────────────────┐
│                    ALGILAMA ALANLARI                    │
├─────────────────────────────────────────────────────────┤
│  ○ Dış Alan (15m) → Alert: Müzik başlar                │
│  ○ İç Alan (7m)   → Chasing: Zombi takip eder          │
│  ○ Saldırı (2m)   → Attacking: Saldırı animasyonu      │
└─────────────────────────────────────────────────────────┘
```

| Durum | Davranış |
|-------|----------|
| **Idle** | Boşta bekler, ara sıra ses çıkarır |
| **Alert** | Dış alanda, müzik çalar ama zombi hareket etmez |
| **Chasing** | İç alanda, zombi oyuncuyu takip eder, müzik hızlanır |
| **Attacking** | Saldırı mesafesinde, saldırı animasyonu oynar |

### Müzik Sistemi

- **Katmanlı Müzik**: Dış ve iç alan müzikleri aynı anda çalabilir
- **Dinamik Hız**: İç alanda müzik hızlanır
- **Fade Geçişleri**: Yumuşak ses/hız geçişleri

### Inspector Ayarları

```
=== OYUNCU REFERANSI ===
- player: Oyuncu Transform (veya "Player" tag ile otomatik bulunur)

=== ALGILAMA ALANLARI ===
- outerDetectionRadius: 15 (Dış alan yarıçapı)
- innerDetectionRadius: 7 (İç alan yarıçapı)
- attackRange: 2 (Saldırı mesafesi)

=== HAREKET AYARLARI ===
- walkSpeed: 1.5
- runSpeed: 4
- rotationSpeed: 2
- runTriggerDistance: 5 (Bu mesafede koşmaya başlar)

=== SES AYARLARI ===
- outerAreaMusic: Dış alan müziği
- innerAreaMusic: İç alan müziği (katman)
- idleSounds[]: Boşta sesleri
- chaseSounds[]: Takip sesleri
- attackSound: Saldırı sesi
- musicVolume: 0.7
- innerMusicVolumeMultiplier: 3 (İç müzik ses çarpanı)
- normalMusicSpeed: 1
- fastMusicSpeed: 1.3

=== DEBUG GÖRSELLEŞTİRME ===
- showDebugCircles: true (Oyun içi algılama çemberleri)
```

### Gereksinimler
- `AudioSource` component (otomatik eklenir)
- `NavMeshAgent` component (opsiyonel - yoksa manuel hareket)
- Sahne üzerinde baked NavMesh

### Public API

```csharp
// Mevcut durumu al
ZombieState state = zombieAI.CurrentState;

// Oyuncuya mesafeyi al
float distance = zombieAI.DistanceToPlayer;

// Algılama yarıçaplarını değiştir
zombieAI.SetDetectionRadii(20f, 10f);

// Debug çemberlerini aç/kapat
zombieAI.ToggleDebugCircles(false);
```

---

## 🚀 Kurulum

1. Her iki scripti `Assets` klasörüne ekleyin
2. **Oyuncu için**:
   - Empty GameObject oluşturun
   - `FPSController` scripti ekleyin
   - Child olarak kamera ekleyin ve Inspector'dan atayın
   - "Player" tag'ı verin

3. **Zombi için**:
   - Zombi modeli import edin
   - `ZombieAI` scripti ekleyin
   - NavMeshAgent ekleyin (opsiyonel)
   - Ses ve animasyon dosyalarını Inspector'dan atayın
   - Sahneyi NavMesh ile bake edin

---

## 📝 Notlar

- Her iki script de **Animator desteği** içerir (opsiyonel)
- Debug görselleştirmeler hem **Gizmos** hem de **Runtime LineRenderer** olarak mevcuttur
- Input System paketi kurulu olmalıdır (`com.unity.inputsystem`)
