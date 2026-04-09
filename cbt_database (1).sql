-- phpMyAdmin SQL Dump
-- version 5.2.2
-- https://www.phpmyadmin.net/
--
-- Host: localhost:3306
-- Generation Time: Mar 09, 2026 at 03:09 AM
-- Server version: 8.0.30
-- PHP Version: 8.3.25

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `cbt_database`
--

-- --------------------------------------------------------

--
-- Table structure for table `cache`
--

CREATE TABLE `cache` (
  `key` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `value` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `expiration` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `cache_locks`
--

CREATE TABLE `cache_locks` (
  `key` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `owner` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `expiration` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `failed_jobs`
--

CREATE TABLE `failed_jobs` (
  `id` bigint UNSIGNED NOT NULL,
  `uuid` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `connection` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `queue` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `payload` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `exception` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `failed_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `jobs`
--

CREATE TABLE `jobs` (
  `id` bigint UNSIGNED NOT NULL,
  `queue` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `payload` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `attempts` tinyint UNSIGNED NOT NULL,
  `reserved_at` int UNSIGNED DEFAULT NULL,
  `available_at` int UNSIGNED NOT NULL,
  `created_at` int UNSIGNED NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `job_batches`
--

CREATE TABLE `job_batches` (
  `id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `total_jobs` int NOT NULL,
  `pending_jobs` int NOT NULL,
  `failed_jobs` int NOT NULL,
  `failed_job_ids` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `options` mediumtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `cancelled_at` int DEFAULT NULL,
  `created_at` int NOT NULL,
  `finished_at` int DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `migrations`
--

CREATE TABLE `migrations` (
  `id` int UNSIGNED NOT NULL,
  `migration` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `batch` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `migrations`
--

INSERT INTO `migrations` (`id`, `migration`, `batch`) VALUES
(1, '0001_01_01_000000_create_users_table', 1),
(2, '0001_01_01_000001_create_cache_table', 1),
(3, '0001_01_01_000002_create_jobs_table', 1),
(4, '2026_02_10_144000_create_t_dosen_table', 1),
(5, '2026_02_10_145220_create_t_prodi_table', 1),
(6, '2026_02_10_145507_create_t_tahun_ajaran_table', 1),
(7, '2026_02_10_150157_create_t_matakuliah_table', 1),
(8, '2026_02_10_150604_create_t_kelas_table', 1),
(9, '2026_02_10_151326_create_t_mahasiswa_table', 1),
(10, '2026_02_10_151509_create_t_user_table', 1),
(11, '2026_02_10_152559_create_t_kelas_matakuliah_table', 1),
(12, '2026_02_10_153003_create_t_ujian_table', 1),
(13, '2026_02_10_154454_create_t_soal_table', 1),
(14, '2026_02_10_160425_create_t_gambar_soal_table', 1),
(15, '2026_02_10_160935_create_t_opsi_jawaban_table', 1),
(16, '2026_02_10_161231_create_t_soal_mahasiswa_table', 1),
(17, '2026_02_10_161701_create_t_ujian_kelas_table', 1),
(18, '2026_02_10_163023_create_t_ujian_mahasiswa_table', 1),
(19, '2026_02_15_072247_add_tahun_ajaran_id_to_t_matakuliah_table', 2),
(20, '2026_02_15_072958_add_shufflesoal_to_t_ujian_table', 3);

-- --------------------------------------------------------

--
-- Table structure for table `password_reset_tokens`
--

CREATE TABLE `password_reset_tokens` (
  `email` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `token` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `sessions`
--

CREATE TABLE `sessions` (
  `id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `user_id` bigint UNSIGNED DEFAULT NULL,
  `ip_address` varchar(45) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `user_agent` text CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci,
  `payload` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `last_activity` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `sessions`
--

INSERT INTO `sessions` (`id`, `user_id`, `ip_address`, `user_agent`, `payload`, `last_activity`) VALUES
('jRNt34beoOE5gFpP3yl6X1SQjKjnlzDEMtt0TNaD', 1, '127.0.0.1', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:147.0) Gecko/20100101 Firefox/147.0', 'YTo2OntzOjY6Il90b2tlbiI7czo0MDoiNDE1YXJXUjZTYVl5dGhrbHptbkRWOEs2VWdZNkhtbXNzcGIzb1hiTSI7czo5OiJfcHJldmlvdXMiO2E6MTp7czozOiJ1cmwiO3M6Mjc6Imh0dHA6Ly8xMjcuMC4wLjE6ODAwMC9rZWxhcyI7fXM6NjoiX2ZsYXNoIjthOjI6e3M6Mzoib2xkIjthOjA6e31zOjM6Im5ldyI7YTowOnt9fXM6NTA6ImxvZ2luX3dlYl81OWJhMzZhZGRjMmIyZjk0MDE1ODBmMDE0YzdmNThlYTRlMzA5ODlkIjtpOjE7czo4OiJ1c2VybmFtZSI7czoxMDoiMjI0MTc2MDAwMyI7czo1OiJsZXZlbCI7czo1OiJkb3NlbiI7fQ==', 1771681871),
('uA3mg3PzYt74BFR0R25CsfWBR0vxsJdOyBTOk8ok', 1, '127.0.0.1', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36 Edg/145.0.0.0', 'YTo2OntzOjY6Il90b2tlbiI7czo0MDoiT3hXUFlhdzBVOUxLY1pXYmVkMVpPZDBOQlBCTkxiRlNzcUVlbVBISCI7czo5OiJfcHJldmlvdXMiO2E6MTp7czozOiJ1cmwiO3M6Mjc6Imh0dHA6Ly8xMjcuMC4wLjE6ODAwMC9rZWxhcyI7fXM6NjoiX2ZsYXNoIjthOjI6e3M6Mzoib2xkIjthOjA6e31zOjM6Im5ldyI7YTowOnt9fXM6NTA6ImxvZ2luX3dlYl81OWJhMzZhZGRjMmIyZjk0MDE1ODBmMDE0YzdmNThlYTRlMzA5ODlkIjtpOjE7czo4OiJ1c2VybmFtZSI7czoxMDoiMjI0MTc2MDAwMyI7czo1OiJsZXZlbCI7czo1OiJkb3NlbiI7fQ==', 1771680416),
('uEezOSTlUbbB6QODAFRA8MZe5oU9u2sTfl3Lnzaj', NULL, '127.0.0.1', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36 Edg/145.0.0.0', 'YTozOntzOjY6Il90b2tlbiI7czo0MDoiNUg5NHhJSEw2YVgwNWhvNENHc0dBVEtNOFE3VkJldWtsVkxIVGI2NSI7czo5OiJfcHJldmlvdXMiO2E6MTp7czozOiJ1cmwiO3M6Mjc6Imh0dHA6Ly8xMjcuMC4wLjE6ODAwMC9sb2dpbiI7fXM6NjoiX2ZsYXNoIjthOjI6e3M6Mzoib2xkIjthOjA6e31zOjM6Im5ldyI7YTowOnt9fX0=', 1771667074),
('Y8HYyMJlLZjxohonMy7KlmfcYOKHWFCk9N6yfary', NULL, '127.0.0.1', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:147.0) Gecko/20100101 Firefox/147.0', 'YTozOntzOjY6Il90b2tlbiI7czo0MDoiUlJ2akVKUkZRVm1RcmNRVVNNTmFoRW5jaTRYb3dUYUg1RU44MGJtSiI7czo5OiJfcHJldmlvdXMiO2E6MTp7czozOiJ1cmwiO3M6MzE6Imh0dHA6Ly8xMjcuMC4wLjE6ODAwMC9tYWhhc2lzd2EiO31zOjY6Il9mbGFzaCI7YToyOntzOjM6Im9sZCI7YTowOnt9czozOiJuZXciO2E6MDp7fX19', 1771695147);

-- --------------------------------------------------------

--
-- Table structure for table `t_dosen`
--

CREATE TABLE `t_dosen` (
  `dosen_id` bigint UNSIGNED NOT NULL,
  `nip` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `nama` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_dosen`
--

INSERT INTO `t_dosen` (`dosen_id`, `nip`, `nama`, `created_at`, `updated_at`) VALUES
(1, '2241760003', 'Dr. Budi Santoso', '2026-02-15 10:31:55', '2026-02-15 10:31:55'),
(3, '198501012010121001', 'Dr. Budi Santoso, M.Kom', '2026-03-02 04:56:11', '2026-03-02 04:56:11');

-- --------------------------------------------------------

--
-- Table structure for table `t_gambar_soal`
--

CREATE TABLE `t_gambar_soal` (
  `gambar_soal_id` bigint UNSIGNED NOT NULL,
  `soal_id` bigint UNSIGNED NOT NULL,
  `file` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_gambar_soal`
--

INSERT INTO `t_gambar_soal` (`gambar_soal_id`, `soal_id`, `file`, `created_at`, `updated_at`) VALUES
(1, 1, 'html_table_example.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(2, 2, 'folder_structure.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(3, 3, 'website_a.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(4, 3, 'website_b.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(6, 4, 'rounded_button.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(7, 9, 'stack_illustration.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(8, 10, 'binary_tree.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(9, 11, 'linkedlist_original.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(10, 12, 'queue_step1.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(11, 12, 'queue_step2.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(12, 12, 'queue_step3.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11');

-- --------------------------------------------------------

--
-- Table structure for table `t_kelas`
--

CREATE TABLE `t_kelas` (
  `kelas_id` bigint UNSIGNED NOT NULL,
  `prodi_id` bigint UNSIGNED NOT NULL,
  `tahun_ajaran_id` bigint UNSIGNED NOT NULL,
  `nama_kelas` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_kelas`
--

INSERT INTO `t_kelas` (`kelas_id`, `prodi_id`, `tahun_ajaran_id`, `nama_kelas`, `created_at`, `updated_at`) VALUES
(4, 2, 9, '1D', '2026-02-17 09:44:54', '2026-02-17 09:44:54'),
(5, 2, 9, '1E', '2026-02-17 09:44:54', '2026-02-17 09:44:54'),
(7, 3, 9, '1A', '2026-02-18 08:01:07', '2026-02-18 08:01:07'),
(8, 3, 9, '1B', '2026-02-18 08:01:07', '2026-02-18 08:01:07'),
(9, 3, 9, '1C', '2026-02-18 08:01:07', '2026-02-18 08:01:07'),
(10, 3, 9, '1D', '2026-02-18 08:01:07', '2026-02-18 08:01:07'),
(12, 2, 9, '2B', '2026-02-18 08:03:42', '2026-02-18 08:03:42'),
(13, 2, 9, '2C', '2026-02-18 08:03:42', '2026-02-18 08:03:42'),
(14, 2, 9, '2D', '2026-02-18 08:03:42', '2026-02-18 08:03:42'),
(15, 2, 11, '2A', '2026-02-18 08:18:20', '2026-02-18 08:18:20'),
(17, 2, 11, '2C', '2026-02-18 08:18:20', '2026-02-18 08:18:20'),
(18, 4, 11, '1A', '2026-02-21 01:48:24', '2026-02-21 01:48:24'),
(21, 7, 13, 'TI-4A', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(22, 7, 13, 'TI-4B', '2026-03-02 04:56:11', '2026-03-02 04:56:11');

-- --------------------------------------------------------

--
-- Table structure for table `t_kelas_matakuliah`
--

CREATE TABLE `t_kelas_matakuliah` (
  `kelas_matakuliah_id` bigint UNSIGNED NOT NULL,
  `dosen_id` bigint UNSIGNED NOT NULL,
  `matakuliah_id` bigint UNSIGNED NOT NULL,
  `kelas_id` bigint UNSIGNED NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `t_mahasiswa`
--

CREATE TABLE `t_mahasiswa` (
  `mahasiswa_id` bigint UNSIGNED NOT NULL,
  `kelas_id` bigint UNSIGNED NOT NULL,
  `nim` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `nama` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_mahasiswa`
--

INSERT INTO `t_mahasiswa` (`mahasiswa_id`, `kelas_id`, `nim`, `nama`, `created_at`, `updated_at`) VALUES
(3, 7, '2241760005', 'hirono', '2026-02-18 08:01:51', '2026-02-18 08:01:51'),
(5, 15, '2241760006', 'kageyama', '2026-02-18 08:18:55', '2026-02-18 08:18:55'),
(10, 5, '224176022', 'contoh user 1', '2026-02-19 07:36:48', '2026-02-19 07:36:48'),
(11, 4, '2241750001', 'jay enhypen', '2026-02-19 23:35:34', '2026-02-19 23:35:34'),
(15, 21, '224176012', 'Ahmad Fauzi', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(16, 21, '224176013', 'Siti Nurhaliza', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(17, 21, '224176014', 'Budi Setiawan', '2026-03-02 04:56:11', '2026-03-02 04:56:11');

-- --------------------------------------------------------

--
-- Table structure for table `t_matakuliah`
--

CREATE TABLE `t_matakuliah` (
  `matakuliah_id` bigint UNSIGNED NOT NULL,
  `prodi_id` bigint UNSIGNED NOT NULL,
  `tahun_ajaran_id` bigint UNSIGNED NOT NULL,
  `kode_matakuliah` varchar(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `nama` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_matakuliah`
--

INSERT INTO `t_matakuliah` (`matakuliah_id`, `prodi_id`, `tahun_ajaran_id`, `kode_matakuliah`, `nama`, `created_at`, `updated_at`) VALUES
(1, 7, 13, 'MK001', 'Pemrograman Web', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(2, 7, 13, 'MK002', 'Basis Data', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(3, 7, 13, 'MK003', 'Struktur Data', '2026-03-02 04:56:11', '2026-03-02 04:56:11');

-- --------------------------------------------------------

--
-- Table structure for table `t_opsi_jawaban`
--

CREATE TABLE `t_opsi_jawaban` (
  `opsi_jawaban_id` bigint UNSIGNED NOT NULL,
  `soal_id` bigint UNSIGNED NOT NULL,
  `jawaban` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `nilai` int NOT NULL,
  `file` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_opsi_jawaban`
--

INSERT INTO `t_opsi_jawaban` (`opsi_jawaban_id`, `soal_id`, `jawaban`, `nilai`, `file`, `created_at`, `updated_at`) VALUES
(1, 1, '<table>', 1, 'tag_table.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(2, 1, '<tr>', 0, 'tag_tr.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(3, 1, '<td>', 0, 'tag_td.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(4, 1, '<div>', 0, 'tag_div.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(8, 2, 'index.php', 1, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(9, 2, 'config.php', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(10, 2, 'database.php', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(11, 2, 'helper.php', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(15, 3, 'Website A', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(16, 3, 'Website B', 1, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(17, 3, 'Keduanya', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(18, 3, 'Tidak ada', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(22, 4, 'border-radius', 1, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(23, 4, 'border-curve', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(24, 4, 'corner-style', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(25, 4, 'edge-radius', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(29, 5, 'push()', 1, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(30, 5, 'pop()', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(31, 5, 'shift()', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(32, 5, 'unshift()', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(36, 6, 'SELECT * FROM mahasiswa', 1, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(37, 6, 'GET * FROM mahasiswa', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(38, 6, 'SHOW * FROM mahasiswa', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(39, 6, 'DISPLAY * FROM mahasiswa', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(43, 7, 'Mengidentifikasi record secara unik', 1, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(44, 7, 'Mengurutkan data', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(45, 7, 'Menghapus data', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(46, 7, 'Membuat backup data', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(50, 8, 'FULL OUTER JOIN', 1, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(51, 8, 'INNER JOIN', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(52, 8, 'LEFT JOIN', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(53, 8, 'RIGHT JOIN', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(57, 9, 'Elemen paling atas (Top)', 1, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(58, 9, 'Elemen paling bawah (Bottom)', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(59, 9, 'Elemen tengah', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(60, 9, 'Semua elemen', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(64, 10, '3', 1, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(65, 10, '2', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(66, 10, '4', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(67, 10, '5', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(71, 11, 'Pilihan A', 0, 'linkedlist_insert_a.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(72, 11, 'Pilihan B', 1, 'linkedlist_insert_b.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(73, 11, 'Pilihan C', 0, 'linkedlist_insert_c.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(74, 11, 'Pilihan D', 0, 'linkedlist_insert_d.jpg', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(78, 12, 'Elemen pertama (Front)', 1, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(79, 12, 'Elemen terakhir (Rear)', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(80, 12, 'Elemen tengah', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(81, 12, 'Elemen terbesar', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(85, 13, 'Vertex', 1, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(86, 13, 'Edge', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(87, 13, 'Node', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(88, 13, 'Path', 0, NULL, '2026-03-02 04:56:11', '2026-03-02 04:56:11');

-- --------------------------------------------------------

--
-- Table structure for table `t_prodi`
--

CREATE TABLE `t_prodi` (
  `prodi_id` bigint UNSIGNED NOT NULL,
  `kode_prodi` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `nama` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `jenjang` enum('D2','D3','D4') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_prodi`
--

INSERT INTO `t_prodi` (`prodi_id`, `kode_prodi`, `nama`, `jenjang`, `created_at`, `updated_at`) VALUES
(2, '22400ABC', 'TI', 'D3', '2026-02-16 08:38:44', '2026-02-18 08:00:31'),
(3, 'PRODIA', 'SIB', 'D3', '2026-02-18 01:53:29', '2026-02-18 08:00:40'),
(4, 'PRODIB', 'COBA PRODI', 'D3', '2026-02-18 01:53:42', '2026-02-21 01:48:02'),
(7, 'TI', 'Teknik Informatika', 'D4', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(8, 'SI', 'Sistem Informasi', 'D3', '2026-03-02 04:56:11', '2026-03-02 04:56:11');

-- --------------------------------------------------------

--
-- Table structure for table `t_soal`
--

CREATE TABLE `t_soal` (
  `soal_id` bigint UNSIGNED NOT NULL,
  `ujian_id` bigint UNSIGNED NOT NULL,
  `kode_soal` varchar(5) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `pertanyaan` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_soal`
--

INSERT INTO `t_soal` (`soal_id`, `ujian_id`, `kode_soal`, `pertanyaan`, `created_at`, `updated_at`) VALUES
(1, 1, 'S01', 'Perhatikan kode HTML berikut! Tag apa yang digunakan untuk membuat tabel?', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(2, 1, 'S02', 'Berdasarkan gambar struktur folder berikut, file mana yang berfungsi sebagai entry point aplikasi?', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(3, 1, 'S03', 'Bandingkan kedua tampilan website berikut. Mana yang menggunakan Bootstrap?', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(4, 1, 'S04', 'Perhatikan tampilan button berikut. Property CSS apa yang digunakan untuk membuat sudut melengkung?', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(5, 1, 'S05', 'Method JavaScript mana yang digunakan untuk menambahkan elemen ke akhir array?', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(6, 2, 'BD01', 'Perintah SQL untuk menampilkan semua data dari tabel mahasiswa adalah?', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(7, 2, 'BD02', 'Apa fungsi PRIMARY KEY dalam database?', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(8, 2, 'BD03', 'Jenis JOIN yang menampilkan semua data dari kedua tabel adalah?', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(9, 3, 'SD01', 'Perhatikan ilustrasi Stack berikut! Apa output jika dilakukan operasi POP?', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(10, 3, 'SD02', 'Berdasarkan Binary Tree berikut, berapa tinggi (height) dari tree tersebut?', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(11, 3, 'SD03', 'Perhatikan Linked List berikut. Manakah ilustrasi yang benar untuk operasi INSERT di tengah?', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(12, 3, 'SD04', 'Perhatikan proses antrian (Queue) berikut. Elemen mana yang akan keluar duluan (DEQUEUE)?', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(13, 3, 'SD05', 'Dalam struktur data Graph, kumpulan simpul disebut sebagai?', '2026-03-02 04:56:11', '2026-03-02 04:56:11');

-- --------------------------------------------------------

--
-- Table structure for table `t_soal_mahasiswa`
--

CREATE TABLE `t_soal_mahasiswa` (
  `soal_mahasiswa_id` bigint UNSIGNED NOT NULL,
  `soal_id` bigint UNSIGNED NOT NULL,
  `mahasiswa_id` bigint UNSIGNED NOT NULL,
  `opsi_jawaban_id` bigint UNSIGNED NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_soal_mahasiswa`
--

INSERT INTO `t_soal_mahasiswa` (`soal_mahasiswa_id`, `soal_id`, `mahasiswa_id`, `opsi_jawaban_id`, `created_at`, `updated_at`) VALUES
(1, 1, 16, 1, NULL, NULL),
(2, 2, 16, 9, NULL, NULL),
(3, 3, 16, 17, NULL, NULL),
(4, 4, 16, 25, NULL, NULL),
(5, 5, 16, 29, NULL, NULL),
(6, 9, 16, 58, NULL, NULL),
(7, 10, 16, 65, NULL, NULL),
(8, 11, 16, 74, NULL, NULL),
(9, 12, 16, 78, NULL, NULL),
(10, 13, 16, 87, NULL, NULL);

-- --------------------------------------------------------

--
-- Table structure for table `t_tahun_ajaran`
--

CREATE TABLE `t_tahun_ajaran` (
  `tahun_ajaran_id` bigint UNSIGNED NOT NULL,
  `tahun` date NOT NULL,
  `semester` enum('ganjil','genap') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_tahun_ajaran`
--

INSERT INTO `t_tahun_ajaran` (`tahun_ajaran_id`, `tahun`, `semester`, `created_at`, `updated_at`) VALUES
(9, '2026-02-13', 'genap', '2026-02-16 07:58:17', '2026-02-16 08:43:32'),
(11, '2025-02-06', 'ganjil', '2026-02-18 01:56:55', '2026-02-18 01:56:55'),
(13, '2024-01-01', 'ganjil', '2026-03-02 04:56:11', '2026-03-02 04:56:11');

-- --------------------------------------------------------

--
-- Table structure for table `t_ujian`
--

CREATE TABLE `t_ujian` (
  `ujian_id` bigint UNSIGNED NOT NULL,
  `matakuliah_id` bigint UNSIGNED NOT NULL,
  `tahun_ajaran_id` bigint UNSIGNED NOT NULL,
  `prodi_id` bigint UNSIGNED NOT NULL,
  `kode_ujian` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `nama_ujian` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `status` enum('menunggu','dimulai','selesai') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'menunggu',
  `shufflesoal` int NOT NULL,
  `starttime` time NOT NULL,
  `endtime` time NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_ujian`
--

INSERT INTO `t_ujian` (`ujian_id`, `matakuliah_id`, `tahun_ajaran_id`, `prodi_id`, `kode_ujian`, `nama_ujian`, `status`, `shufflesoal`, `starttime`, `endtime`, `created_at`, `updated_at`) VALUES
(1, 1, 13, 7, 'UJN001', 'UTS Pemrograman Web', 'dimulai', 0, '08:00:00', '10:00:00', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(2, 2, 13, 7, 'UJN002', 'UAS Basis Data', 'dimulai', 1, '13:00:00', '15:00:00', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(3, 3, 13, 7, 'UJN003', 'Quiz Struktur Data', 'dimulai', 0, '10:00:00', '11:00:00', '2026-03-02 04:56:11', '2026-03-02 04:56:11');

-- --------------------------------------------------------

--
-- Table structure for table `t_ujian_kelas`
--

CREATE TABLE `t_ujian_kelas` (
  `ujian_kelas_id` bigint UNSIGNED NOT NULL,
  `ujian_id` bigint UNSIGNED NOT NULL,
  `kelas_id` bigint UNSIGNED NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_ujian_kelas`
--

INSERT INTO `t_ujian_kelas` (`ujian_kelas_id`, `ujian_id`, `kelas_id`, `created_at`, `updated_at`) VALUES
(1, 1, 21, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(2, 2, 21, '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(3, 3, 21, '2026-03-02 04:56:11', '2026-03-02 04:56:11');

-- --------------------------------------------------------

--
-- Table structure for table `t_ujian_mahasiswa`
--

CREATE TABLE `t_ujian_mahasiswa` (
  `ujianmahasiswa_id` bigint UNSIGNED NOT NULL,
  `ujian_id` bigint UNSIGNED NOT NULL,
  `mahasiswa_id` bigint UNSIGNED NOT NULL,
  `starttime` time DEFAULT NULL,
  `endtime` time DEFAULT NULL,
  `extendtime` time DEFAULT NULL,
  `tanggal_ujian` datetime DEFAULT NULL,
  `status` enum('menunggu','dimulai','selesai','dihentikan') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `keterangan` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `nilai` int DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_ujian_mahasiswa`
--

INSERT INTO `t_ujian_mahasiswa` (`ujianmahasiswa_id`, `ujian_id`, `mahasiswa_id`, `starttime`, `endtime`, `extendtime`, `tanggal_ujian`, `status`, `keterangan`, `nilai`, `created_at`, `updated_at`) VALUES
(1, 1, 16, '12:08:22', '11:27:34', NULL, '2026-03-02 12:08:22', 'selesai', NULL, NULL, NULL, NULL),
(2, 3, 16, '11:13:36', '11:14:17', NULL, '2026-03-05 11:13:36', 'selesai', NULL, NULL, NULL, NULL),
(3, 1, 16, '11:26:39', '11:27:34', NULL, '2026-03-05 11:26:39', 'selesai', NULL, NULL, NULL, NULL);

-- --------------------------------------------------------

--
-- Table structure for table `t_user`
--

CREATE TABLE `t_user` (
  `user_id` bigint UNSIGNED NOT NULL,
  `dosen_id` bigint UNSIGNED DEFAULT NULL,
  `mahasiswa_id` bigint UNSIGNED DEFAULT NULL,
  `username` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `password` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `level` enum('mahasiswa','dosen','panitia') CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_user`
--

INSERT INTO `t_user` (`user_id`, `dosen_id`, `mahasiswa_id`, `username`, `password`, `level`, `created_at`, `updated_at`) VALUES
(1, 1, NULL, '2241760003', '$2y$12$cxAushHEmhb6HpwBvfoAH.F3Xu.cGEHR1h3nuO7Lwy6H2Dfs2ylO6', 'dosen', '2026-02-15 10:34:17', '2026-02-15 10:34:17'),
(4, NULL, 10, '224176022', '$2y$12$p8J2J2eWKsJ2N.VfTexlZOBUj91pl1hOLuqssgLeL6k7kw5nB3T7e', 'mahasiswa', '2026-02-19 07:36:49', '2026-02-21 02:41:42'),
(5, NULL, 11, '2241750001', '$2y$12$FDhYLI703ljV./WOjSg/Rury9OTki18zn7CHJP.7zT5mD6RUi.Dci', 'mahasiswa', '2026-02-19 23:35:34', '2026-02-19 23:35:34'),
(8, 3, NULL, 'dosen001', 'password123', 'dosen', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(9, NULL, 15, '224176012', 'password123', 'mahasiswa', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(10, NULL, 16, '224176013', 'password123', 'mahasiswa', '2026-03-02 04:56:11', '2026-03-02 04:56:11'),
(11, NULL, 17, '224176014', 'password123', 'mahasiswa', '2026-03-02 04:56:11', '2026-03-02 04:56:11');

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `id` bigint UNSIGNED NOT NULL,
  `name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `email` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `email_verified_at` timestamp NULL DEFAULT NULL,
  `password` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL,
  `remember_token` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `cache`
--
ALTER TABLE `cache`
  ADD PRIMARY KEY (`key`);

--
-- Indexes for table `cache_locks`
--
ALTER TABLE `cache_locks`
  ADD PRIMARY KEY (`key`);

--
-- Indexes for table `failed_jobs`
--
ALTER TABLE `failed_jobs`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `failed_jobs_uuid_unique` (`uuid`);

--
-- Indexes for table `jobs`
--
ALTER TABLE `jobs`
  ADD PRIMARY KEY (`id`),
  ADD KEY `jobs_queue_index` (`queue`);

--
-- Indexes for table `job_batches`
--
ALTER TABLE `job_batches`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `migrations`
--
ALTER TABLE `migrations`
  ADD PRIMARY KEY (`id`);

--
-- Indexes for table `password_reset_tokens`
--
ALTER TABLE `password_reset_tokens`
  ADD PRIMARY KEY (`email`);

--
-- Indexes for table `sessions`
--
ALTER TABLE `sessions`
  ADD PRIMARY KEY (`id`),
  ADD KEY `sessions_user_id_index` (`user_id`),
  ADD KEY `sessions_last_activity_index` (`last_activity`);

--
-- Indexes for table `t_dosen`
--
ALTER TABLE `t_dosen`
  ADD PRIMARY KEY (`dosen_id`);

--
-- Indexes for table `t_gambar_soal`
--
ALTER TABLE `t_gambar_soal`
  ADD PRIMARY KEY (`gambar_soal_id`),
  ADD KEY `t_gambar_soal_soal_id_foreign` (`soal_id`);

--
-- Indexes for table `t_kelas`
--
ALTER TABLE `t_kelas`
  ADD PRIMARY KEY (`kelas_id`),
  ADD KEY `t_kelas_prodi_id_foreign` (`prodi_id`),
  ADD KEY `t_kelas_tahun_ajaran_id_foreign` (`tahun_ajaran_id`);

--
-- Indexes for table `t_kelas_matakuliah`
--
ALTER TABLE `t_kelas_matakuliah`
  ADD PRIMARY KEY (`kelas_matakuliah_id`),
  ADD KEY `t_kelas_matakuliah_dosen_id_foreign` (`dosen_id`),
  ADD KEY `t_kelas_matakuliah_matakuliah_id_foreign` (`matakuliah_id`),
  ADD KEY `t_kelas_matakuliah_kelas_id_foreign` (`kelas_id`);

--
-- Indexes for table `t_mahasiswa`
--
ALTER TABLE `t_mahasiswa`
  ADD PRIMARY KEY (`mahasiswa_id`),
  ADD KEY `t_mahasiswa_kelas_id_foreign` (`kelas_id`);

--
-- Indexes for table `t_matakuliah`
--
ALTER TABLE `t_matakuliah`
  ADD PRIMARY KEY (`matakuliah_id`),
  ADD UNIQUE KEY `t_matakuliah_kode_matakuliah_unique` (`kode_matakuliah`),
  ADD KEY `t_matakuliah_prodi_id_foreign` (`prodi_id`),
  ADD KEY `t_matakuliah_tahun_ajaran_id_foreign` (`tahun_ajaran_id`);

--
-- Indexes for table `t_opsi_jawaban`
--
ALTER TABLE `t_opsi_jawaban`
  ADD PRIMARY KEY (`opsi_jawaban_id`),
  ADD KEY `t_opsi_jawaban_soal_id_foreign` (`soal_id`);

--
-- Indexes for table `t_prodi`
--
ALTER TABLE `t_prodi`
  ADD PRIMARY KEY (`prodi_id`);

--
-- Indexes for table `t_soal`
--
ALTER TABLE `t_soal`
  ADD PRIMARY KEY (`soal_id`),
  ADD KEY `t_soal_ujian_id_foreign` (`ujian_id`);

--
-- Indexes for table `t_soal_mahasiswa`
--
ALTER TABLE `t_soal_mahasiswa`
  ADD PRIMARY KEY (`soal_mahasiswa_id`),
  ADD KEY `t_soal_mahasiswa_soal_id_foreign` (`soal_id`),
  ADD KEY `t_soal_mahasiswa_mahasiswa_id_foreign` (`mahasiswa_id`),
  ADD KEY `t_soal_mahasiswa_opsi_jawaban_id_foreign` (`opsi_jawaban_id`);

--
-- Indexes for table `t_tahun_ajaran`
--
ALTER TABLE `t_tahun_ajaran`
  ADD PRIMARY KEY (`tahun_ajaran_id`);

--
-- Indexes for table `t_ujian`
--
ALTER TABLE `t_ujian`
  ADD PRIMARY KEY (`ujian_id`),
  ADD KEY `t_ujian_matakuliah_id_foreign` (`matakuliah_id`),
  ADD KEY `t_ujian_tahun_ajaran_id_foreign` (`tahun_ajaran_id`),
  ADD KEY `t_ujian_prodi_id_foreign` (`prodi_id`);

--
-- Indexes for table `t_ujian_kelas`
--
ALTER TABLE `t_ujian_kelas`
  ADD PRIMARY KEY (`ujian_kelas_id`),
  ADD KEY `t_ujian_kelas_ujian_id_foreign` (`ujian_id`),
  ADD KEY `t_ujian_kelas_kelas_id_foreign` (`kelas_id`);

--
-- Indexes for table `t_ujian_mahasiswa`
--
ALTER TABLE `t_ujian_mahasiswa`
  ADD PRIMARY KEY (`ujianmahasiswa_id`),
  ADD KEY `t_ujian_mahasiswa_ujian_id_foreign` (`ujian_id`),
  ADD KEY `t_ujian_mahasiswa_mahasiswa_id_foreign` (`mahasiswa_id`);

--
-- Indexes for table `t_user`
--
ALTER TABLE `t_user`
  ADD PRIMARY KEY (`user_id`),
  ADD UNIQUE KEY `t_user_username_unique` (`username`),
  ADD KEY `t_user_dosen_id_foreign` (`dosen_id`),
  ADD KEY `t_user_mahasiswa_id_foreign` (`mahasiswa_id`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `users_email_unique` (`email`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `failed_jobs`
--
ALTER TABLE `failed_jobs`
  MODIFY `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `jobs`
--
ALTER TABLE `jobs`
  MODIFY `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `migrations`
--
ALTER TABLE `migrations`
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=21;

--
-- AUTO_INCREMENT for table `t_dosen`
--
ALTER TABLE `t_dosen`
  MODIFY `dosen_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT for table `t_gambar_soal`
--
ALTER TABLE `t_gambar_soal`
  MODIFY `gambar_soal_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=13;

--
-- AUTO_INCREMENT for table `t_kelas`
--
ALTER TABLE `t_kelas`
  MODIFY `kelas_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=23;

--
-- AUTO_INCREMENT for table `t_kelas_matakuliah`
--
ALTER TABLE `t_kelas_matakuliah`
  MODIFY `kelas_matakuliah_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `t_mahasiswa`
--
ALTER TABLE `t_mahasiswa`
  MODIFY `mahasiswa_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=18;

--
-- AUTO_INCREMENT for table `t_matakuliah`
--
ALTER TABLE `t_matakuliah`
  MODIFY `matakuliah_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT for table `t_opsi_jawaban`
--
ALTER TABLE `t_opsi_jawaban`
  MODIFY `opsi_jawaban_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=89;

--
-- AUTO_INCREMENT for table `t_prodi`
--
ALTER TABLE `t_prodi`
  MODIFY `prodi_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=9;

--
-- AUTO_INCREMENT for table `t_soal`
--
ALTER TABLE `t_soal`
  MODIFY `soal_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=14;

--
-- AUTO_INCREMENT for table `t_soal_mahasiswa`
--
ALTER TABLE `t_soal_mahasiswa`
  MODIFY `soal_mahasiswa_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=11;

--
-- AUTO_INCREMENT for table `t_tahun_ajaran`
--
ALTER TABLE `t_tahun_ajaran`
  MODIFY `tahun_ajaran_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=14;

--
-- AUTO_INCREMENT for table `t_ujian`
--
ALTER TABLE `t_ujian`
  MODIFY `ujian_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT for table `t_ujian_kelas`
--
ALTER TABLE `t_ujian_kelas`
  MODIFY `ujian_kelas_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT for table `t_ujian_mahasiswa`
--
ALTER TABLE `t_ujian_mahasiswa`
  MODIFY `ujianmahasiswa_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- AUTO_INCREMENT for table `t_user`
--
ALTER TABLE `t_user`
  MODIFY `user_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=12;

--
-- AUTO_INCREMENT for table `users`
--
ALTER TABLE `users`
  MODIFY `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `t_gambar_soal`
--
ALTER TABLE `t_gambar_soal`
  ADD CONSTRAINT `t_gambar_soal_soal_id_foreign` FOREIGN KEY (`soal_id`) REFERENCES `t_soal` (`soal_id`) ON DELETE CASCADE;

--
-- Constraints for table `t_kelas`
--
ALTER TABLE `t_kelas`
  ADD CONSTRAINT `t_kelas_prodi_id_foreign` FOREIGN KEY (`prodi_id`) REFERENCES `t_prodi` (`prodi_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `t_kelas_tahun_ajaran_id_foreign` FOREIGN KEY (`tahun_ajaran_id`) REFERENCES `t_tahun_ajaran` (`tahun_ajaran_id`) ON DELETE CASCADE;

--
-- Constraints for table `t_kelas_matakuliah`
--
ALTER TABLE `t_kelas_matakuliah`
  ADD CONSTRAINT `t_kelas_matakuliah_dosen_id_foreign` FOREIGN KEY (`dosen_id`) REFERENCES `t_dosen` (`dosen_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `t_kelas_matakuliah_kelas_id_foreign` FOREIGN KEY (`kelas_id`) REFERENCES `t_kelas` (`kelas_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `t_kelas_matakuliah_matakuliah_id_foreign` FOREIGN KEY (`matakuliah_id`) REFERENCES `t_matakuliah` (`matakuliah_id`) ON DELETE CASCADE;

--
-- Constraints for table `t_mahasiswa`
--
ALTER TABLE `t_mahasiswa`
  ADD CONSTRAINT `t_mahasiswa_kelas_id_foreign` FOREIGN KEY (`kelas_id`) REFERENCES `t_kelas` (`kelas_id`) ON DELETE CASCADE;

--
-- Constraints for table `t_matakuliah`
--
ALTER TABLE `t_matakuliah`
  ADD CONSTRAINT `t_matakuliah_prodi_id_foreign` FOREIGN KEY (`prodi_id`) REFERENCES `t_prodi` (`prodi_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `t_matakuliah_tahun_ajaran_id_foreign` FOREIGN KEY (`tahun_ajaran_id`) REFERENCES `t_tahun_ajaran` (`tahun_ajaran_id`) ON DELETE CASCADE;

--
-- Constraints for table `t_opsi_jawaban`
--
ALTER TABLE `t_opsi_jawaban`
  ADD CONSTRAINT `t_opsi_jawaban_soal_id_foreign` FOREIGN KEY (`soal_id`) REFERENCES `t_soal` (`soal_id`) ON DELETE CASCADE;

--
-- Constraints for table `t_soal`
--
ALTER TABLE `t_soal`
  ADD CONSTRAINT `t_soal_ujian_id_foreign` FOREIGN KEY (`ujian_id`) REFERENCES `t_ujian` (`ujian_id`) ON DELETE CASCADE;

--
-- Constraints for table `t_soal_mahasiswa`
--
ALTER TABLE `t_soal_mahasiswa`
  ADD CONSTRAINT `t_soal_mahasiswa_mahasiswa_id_foreign` FOREIGN KEY (`mahasiswa_id`) REFERENCES `t_mahasiswa` (`mahasiswa_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `t_soal_mahasiswa_opsi_jawaban_id_foreign` FOREIGN KEY (`opsi_jawaban_id`) REFERENCES `t_opsi_jawaban` (`opsi_jawaban_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `t_soal_mahasiswa_soal_id_foreign` FOREIGN KEY (`soal_id`) REFERENCES `t_soal` (`soal_id`) ON DELETE CASCADE;

--
-- Constraints for table `t_ujian`
--
ALTER TABLE `t_ujian`
  ADD CONSTRAINT `t_ujian_matakuliah_id_foreign` FOREIGN KEY (`matakuliah_id`) REFERENCES `t_matakuliah` (`matakuliah_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `t_ujian_prodi_id_foreign` FOREIGN KEY (`prodi_id`) REFERENCES `t_prodi` (`prodi_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `t_ujian_tahun_ajaran_id_foreign` FOREIGN KEY (`tahun_ajaran_id`) REFERENCES `t_tahun_ajaran` (`tahun_ajaran_id`) ON DELETE CASCADE;

--
-- Constraints for table `t_ujian_kelas`
--
ALTER TABLE `t_ujian_kelas`
  ADD CONSTRAINT `t_ujian_kelas_kelas_id_foreign` FOREIGN KEY (`kelas_id`) REFERENCES `t_kelas` (`kelas_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `t_ujian_kelas_ujian_id_foreign` FOREIGN KEY (`ujian_id`) REFERENCES `t_ujian` (`ujian_id`) ON DELETE CASCADE;

--
-- Constraints for table `t_ujian_mahasiswa`
--
ALTER TABLE `t_ujian_mahasiswa`
  ADD CONSTRAINT `t_ujian_mahasiswa_mahasiswa_id_foreign` FOREIGN KEY (`mahasiswa_id`) REFERENCES `t_mahasiswa` (`mahasiswa_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `t_ujian_mahasiswa_ujian_id_foreign` FOREIGN KEY (`ujian_id`) REFERENCES `t_ujian` (`ujian_id`) ON DELETE CASCADE;

--
-- Constraints for table `t_user`
--
ALTER TABLE `t_user`
  ADD CONSTRAINT `t_user_dosen_id_foreign` FOREIGN KEY (`dosen_id`) REFERENCES `t_dosen` (`dosen_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `t_user_mahasiswa_id_foreign` FOREIGN KEY (`mahasiswa_id`) REFERENCES `t_mahasiswa` (`mahasiswa_id`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
