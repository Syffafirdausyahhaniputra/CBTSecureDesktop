# Logo Polinema

## Cara Menambahkan Logo:

1. **Download logo Polinema** dari website resmi: https://polinema.ac.id
   - Format yang disarankan: PNG dengan background transparan
   - Ukuran: minimal 512x512 pixels

2. **Simpan file logo** dengan nama `polinema-logo.png` di folder `Assets/Images/`

3. **Set Build Action**:
   - Klik kanan pada file logo di Solution Explorer
   - Pilih "Properties"
   - Set "Build Action" = "Resource"
   - Set "Copy to Output Directory" = "Copy if newer"

## Lokasi Logo di UI:

- **LoginWindow**: Header (80x80 pixels)
- **DashboardWindow**: Header (60x60 pixels)
- **ExamWindow**: Footer watermark (40x40 pixels)

## Alternative:

Jika tidak memiliki logo resmi, Anda bisa menggunakan placeholder atau text "POLINEMA" dengan styling khusus.

File ini akan otomatis terdeteksi oleh aplikasi setelah ditambahkan.
