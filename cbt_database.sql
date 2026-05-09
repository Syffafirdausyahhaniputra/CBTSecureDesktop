-- phpMyAdmin SQL Dump
-- version 5.2.2
-- https://www.phpmyadmin.net/
--
-- Host: localhost:3306
-- Generation Time: May 08, 2026 at 02:08 AM
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
  `key` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `value` mediumtext COLLATE utf8mb4_unicode_ci NOT NULL,
  `expiration` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `cache_locks`
--

CREATE TABLE `cache_locks` (
  `key` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `owner` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `expiration` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `failed_jobs`
--

CREATE TABLE `failed_jobs` (
  `id` bigint UNSIGNED NOT NULL,
  `uuid` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `connection` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `queue` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `payload` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
  `exception` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
  `failed_at` timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `jobs`
--

CREATE TABLE `jobs` (
  `id` bigint UNSIGNED NOT NULL,
  `queue` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `payload` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
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
  `id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `total_jobs` int NOT NULL,
  `pending_jobs` int NOT NULL,
  `failed_jobs` int NOT NULL,
  `failed_job_ids` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
  `options` mediumtext COLLATE utf8mb4_unicode_ci,
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
  `migration` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
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
(19, '2026_02_15_072247_add_tahun_ajaran_id_to_t_matakuliah_table', 1),
(20, '2026_02_15_072958_add_shufflesoal_to_t_ujian_table', 1),
(21, '2026_02_25_032559_change_dosen_id_nullable_in_t_kelas_matakuliah', 1),
(22, '2026_02_25_154802_change_fk_matakuliah_prodi_delete_rule', 1),
(23, '2026_02_25_160926_change_fk_kelas_delete_rules_to_restrict', 1),
(24, '2026_02_25_161102_change_fk_mahasiswa_kelas_to_restrict', 1),
(25, '2026_03_03_122732_create_t_kelas_mahasiswa_table', 1),
(26, '2026_03_03_134157_change_tahun_column_type_in_t_tahun_ajaran_table', 1),
(27, '2026_03_04_073607_drop_kelas_id_from_t_mahasiswa', 1),
(28, '2026_03_21_133702_change_time_to_datetime_in_t_ujian', 1),
(29, '2026_03_23_205251_modify_kode_soal_to_nomer_soal_in_t_soal', 1),
(30, '2026_04_01_153715_alter_typedata_pertanyaan_to_longtext_on_t_soal', 1),
(31, '2026_04_01_153758_alter_typedata_jawaban_to_longtext_on_t_opsi_jawaban', 1),
(32, '2026_04_26_060138_change_kode_matakuliah_length_on_t_matakuliah_table', 1),
(33, '2026_05_01_195517_add_foto_to_t_user_table', 1),
(34, '2026_05_05_220131_change_extendtime_datatype_on_t_ujian_mahasiswa', 1),
(35, '2026_05_07_013447_create_personal_access_tokens_table', 1);

-- --------------------------------------------------------

--
-- Table structure for table `password_reset_tokens`
--

CREATE TABLE `password_reset_tokens` (
  `email` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `token` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `personal_access_tokens`
--

CREATE TABLE `personal_access_tokens` (
  `id` bigint UNSIGNED NOT NULL,
  `tokenable_type` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `tokenable_id` bigint UNSIGNED NOT NULL,
  `name` text COLLATE utf8mb4_unicode_ci NOT NULL,
  `token` varchar(64) COLLATE utf8mb4_unicode_ci NOT NULL,
  `abilities` text COLLATE utf8mb4_unicode_ci,
  `last_used_at` timestamp NULL DEFAULT NULL,
  `expires_at` timestamp NULL DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `sessions`
--

CREATE TABLE `sessions` (
  `id` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `user_id` bigint UNSIGNED DEFAULT NULL,
  `ip_address` varchar(45) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `user_agent` text COLLATE utf8mb4_unicode_ci,
  `payload` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
  `last_activity` int NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `sessions`
--

INSERT INTO `sessions` (`id`, `user_id`, `ip_address`, `user_agent`, `payload`, `last_activity`) VALUES
('YSOOkZu3QkYgTmoEqBvAhSHmweYUbojFEMjmcSaR', 1, '127.0.0.1', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/147.0.0.0 Safari/537.36 Edg/147.0.0.0', 'YTo3OntzOjY6Il90b2tlbiI7czo0MDoibXNJQzBTNER2RUczSVczdjF5eGNxMER6Y2JyVW9NRlZWbTBKemtxbCI7czo2OiJfZmxhc2giO2E6Mjp7czozOiJvbGQiO2E6MDp7fXM6MzoibmV3IjthOjA6e319czozOiJ1cmwiO2E6MTp7czo4OiJpbnRlbmRlZCI7czoyNzoiaHR0cDovLzEyNy4wLjAuMTo4MDAwL3VqaWFuIjt9czo5OiJfcHJldmlvdXMiO2E6MTp7czozOiJ1cmwiO3M6MzQ6Imh0dHA6Ly8xMjcuMC4wLjE6ODAwMC91amlhbi8xL3NvYWwiO31zOjUwOiJsb2dpbl93ZWJfNTliYTM2YWRkYzJiMmY5NDAxNTgwZjAxNGM3ZjU4ZWE0ZTMwOTg5ZCI7aToxO3M6ODoidXNlcm5hbWUiO3M6MTA6IjIyNDE3NjAwMDMiO3M6NToibGV2ZWwiO3M6NToiZG9zZW4iO30=', 1778120356);

-- --------------------------------------------------------

--
-- Table structure for table `t_dosen`
--

CREATE TABLE `t_dosen` (
  `dosen_id` bigint UNSIGNED NOT NULL,
  `nip` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `nama` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_dosen`
--

INSERT INTO `t_dosen` (`dosen_id`, `nip`, `nama`, `created_at`, `updated_at`) VALUES
(1, '2241760003', 'Dr. Budi Santoso', '2026-05-07 02:16:36', '2026-05-07 02:16:36');

-- --------------------------------------------------------

--
-- Table structure for table `t_gambar_soal`
--

CREATE TABLE `t_gambar_soal` (
  `gambar_soal_id` bigint UNSIGNED NOT NULL,
  `soal_id` bigint UNSIGNED NOT NULL,
  `file` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_gambar_soal`
--

INSERT INTO `t_gambar_soal` (`gambar_soal_id`, `soal_id`, `file`, `created_at`, `updated_at`) VALUES
(1, 1, 'gambar_soal/1/X664Xs6oKEACkLuk70kb8SiHXFad8DX5kxKmWyfG.png', '2026-05-07 02:18:11', '2026-05-07 02:18:11');

-- --------------------------------------------------------

--
-- Table structure for table `t_kelas`
--

CREATE TABLE `t_kelas` (
  `kelas_id` bigint UNSIGNED NOT NULL,
  `prodi_id` bigint UNSIGNED NOT NULL,
  `tahun_ajaran_id` bigint UNSIGNED NOT NULL,
  `nama_kelas` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_kelas`
--

INSERT INTO `t_kelas` (`kelas_id`, `prodi_id`, `tahun_ajaran_id`, `nama_kelas`, `created_at`, `updated_at`) VALUES
(1, 1, 1, 'TI-2A', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(2, 2, 1, 'SI-2A', '2026-05-07 02:16:36', '2026-05-07 02:16:36');

-- --------------------------------------------------------

--
-- Table structure for table `t_kelas_mahasiswa`
--

CREATE TABLE `t_kelas_mahasiswa` (
  `kelas_mahasiswa_id` bigint UNSIGNED NOT NULL,
  `mahasiswa_id` bigint UNSIGNED NOT NULL,
  `kelas_id` bigint UNSIGNED NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_kelas_mahasiswa`
--

INSERT INTO `t_kelas_mahasiswa` (`kelas_mahasiswa_id`, `mahasiswa_id`, `kelas_id`, `created_at`, `updated_at`) VALUES
(1, 1, 1, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(2, 2, 2, '2026-05-07 02:16:36', '2026-05-07 02:16:36');

-- --------------------------------------------------------

--
-- Table structure for table `t_kelas_matakuliah`
--

CREATE TABLE `t_kelas_matakuliah` (
  `kelas_matakuliah_id` bigint UNSIGNED NOT NULL,
  `dosen_id` bigint UNSIGNED DEFAULT NULL,
  `matakuliah_id` bigint UNSIGNED NOT NULL,
  `kelas_id` bigint UNSIGNED NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_kelas_matakuliah`
--

INSERT INTO `t_kelas_matakuliah` (`kelas_matakuliah_id`, `dosen_id`, `matakuliah_id`, `kelas_id`, `created_at`, `updated_at`) VALUES
(1, 1, 1, 1, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(2, 1, 2, 2, '2026-05-07 02:16:36', '2026-05-07 02:16:36');

-- --------------------------------------------------------

--
-- Table structure for table `t_mahasiswa`
--

CREATE TABLE `t_mahasiswa` (
  `mahasiswa_id` bigint UNSIGNED NOT NULL,
  `nim` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `nama` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_mahasiswa`
--

INSERT INTO `t_mahasiswa` (`mahasiswa_id`, `nim`, `nama`, `created_at`, `updated_at`) VALUES
(1, '2241760001', 'Ani', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(2, '2241760002', 'Budi', '2026-05-07 02:16:36', '2026-05-07 02:16:36');

-- --------------------------------------------------------

--
-- Table structure for table `t_matakuliah`
--

CREATE TABLE `t_matakuliah` (
  `matakuliah_id` bigint UNSIGNED NOT NULL,
  `prodi_id` bigint UNSIGNED NOT NULL,
  `tahun_ajaran_id` bigint UNSIGNED NOT NULL,
  `kode_matakuliah` varchar(10) COLLATE utf8mb4_unicode_ci NOT NULL,
  `nama` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_matakuliah`
--

INSERT INTO `t_matakuliah` (`matakuliah_id`, `prodi_id`, `tahun_ajaran_id`, `kode_matakuliah`, `nama`, `created_at`, `updated_at`) VALUES
(1, 1, 1, 'MK001', 'Pemrograman Web', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(2, 2, 1, 'MK002', 'Basis Data', '2026-05-07 02:16:36', '2026-05-07 02:16:36');

-- --------------------------------------------------------

--
-- Table structure for table `t_opsi_jawaban`
--

CREATE TABLE `t_opsi_jawaban` (
  `opsi_jawaban_id` bigint UNSIGNED NOT NULL,
  `soal_id` bigint UNSIGNED NOT NULL,
  `jawaban` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
  `nilai` int NOT NULL,
  `file` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_opsi_jawaban`
--

INSERT INTO `t_opsi_jawaban` (`opsi_jawaban_id`, `soal_id`, `jawaban`, `nilai`, `file`, `created_at`, `updated_at`) VALUES
(1, 1, 'Opsi 1 untuk soal 1', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:18:11'),
(2, 1, 'Opsi 2 untuk soal 1', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(3, 1, 'Opsi 3 untuk soal 1', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(4, 1, 'Opsi 4 untuk soal 1', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(5, 1, 'Opsi 5 untuk soal 1', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(6, 2, 'Opsi 1 untuk soal 2', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(7, 2, 'Opsi 2 untuk soal 2', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(8, 2, 'Opsi 3 untuk soal 2', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(9, 2, 'Opsi 4 untuk soal 2', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(10, 2, 'Opsi 5 untuk soal 2', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(11, 3, 'Opsi 1 untuk soal 3', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(12, 3, 'Opsi 2 untuk soal 3', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(13, 3, 'Opsi 3 untuk soal 3', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(14, 3, 'Opsi 4 untuk soal 3', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(15, 3, 'Opsi 5 untuk soal 3', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(16, 4, 'Opsi 1 untuk soal 4', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(17, 4, 'Opsi 2 untuk soal 4', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(18, 4, 'Opsi 3 untuk soal 4', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(19, 4, 'Opsi 4 untuk soal 4', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(20, 4, 'Opsi 5 untuk soal 4', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(21, 5, 'Opsi 1 untuk soal 5', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(22, 5, 'Opsi 2 untuk soal 5', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(23, 5, 'Opsi 3 untuk soal 5', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(24, 5, 'Opsi 4 untuk soal 5', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(25, 5, 'Opsi 5 untuk soal 5', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(26, 6, 'Opsi 1 untuk soal 6', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(27, 6, 'Opsi 2 untuk soal 6', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(28, 6, 'Opsi 3 untuk soal 6', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(29, 6, 'Opsi 4 untuk soal 6', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(30, 6, 'Opsi 5 untuk soal 6', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(31, 7, 'Opsi 1 untuk soal 7', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(32, 7, 'Opsi 2 untuk soal 7', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(33, 7, 'Opsi 3 untuk soal 7', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(34, 7, 'Opsi 4 untuk soal 7', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(35, 7, 'Opsi 5 untuk soal 7', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(36, 8, 'Opsi 1 untuk soal 8', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(37, 8, 'Opsi 2 untuk soal 8', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(38, 8, 'Opsi 3 untuk soal 8', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(39, 8, 'Opsi 4 untuk soal 8', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(40, 8, 'Opsi 5 untuk soal 8', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(41, 9, 'Opsi 1 untuk soal 9', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(42, 9, 'Opsi 2 untuk soal 9', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(43, 9, 'Opsi 3 untuk soal 9', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(44, 9, 'Opsi 4 untuk soal 9', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(45, 9, 'Opsi 5 untuk soal 9', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(46, 10, 'Opsi 1 untuk soal 10', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(47, 10, 'Opsi 2 untuk soal 10', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(48, 10, 'Opsi 3 untuk soal 10', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(49, 10, 'Opsi 4 untuk soal 10', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(50, 10, 'Opsi 5 untuk soal 10', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(51, 11, 'Opsi 1 untuk soal 11', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(52, 11, 'Opsi 2 untuk soal 11', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(53, 11, 'Opsi 3 untuk soal 11', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(54, 11, 'Opsi 4 untuk soal 11', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(55, 11, 'Opsi 5 untuk soal 11', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(56, 12, 'Opsi 1 untuk soal 12', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(57, 12, 'Opsi 2 untuk soal 12', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(58, 12, 'Opsi 3 untuk soal 12', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(59, 12, 'Opsi 4 untuk soal 12', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(60, 12, 'Opsi 5 untuk soal 12', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(61, 13, 'Opsi 1 untuk soal 13', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(62, 13, 'Opsi 2 untuk soal 13', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(63, 13, 'Opsi 3 untuk soal 13', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(64, 13, 'Opsi 4 untuk soal 13', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(65, 13, 'Opsi 5 untuk soal 13', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(66, 14, 'Opsi 1 untuk soal 14', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(67, 14, 'Opsi 2 untuk soal 14', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(68, 14, 'Opsi 3 untuk soal 14', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(69, 14, 'Opsi 4 untuk soal 14', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(70, 14, 'Opsi 5 untuk soal 14', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(71, 15, 'Opsi 1 untuk soal 15', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(72, 15, 'Opsi 2 untuk soal 15', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(73, 15, 'Opsi 3 untuk soal 15', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(74, 15, 'Opsi 4 untuk soal 15', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(75, 15, 'Opsi 5 untuk soal 15', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(76, 16, 'Opsi 1 untuk soal 16', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(77, 16, 'Opsi 2 untuk soal 16', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(78, 16, 'Opsi 3 untuk soal 16', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(79, 16, 'Opsi 4 untuk soal 16', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(80, 16, 'Opsi 5 untuk soal 16', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(81, 17, 'Opsi 1 untuk soal 17', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(82, 17, 'Opsi 2 untuk soal 17', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(83, 17, 'Opsi 3 untuk soal 17', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(84, 17, 'Opsi 4 untuk soal 17', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(85, 17, 'Opsi 5 untuk soal 17', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(86, 18, 'Opsi 1 untuk soal 18', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(87, 18, 'Opsi 2 untuk soal 18', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(88, 18, 'Opsi 3 untuk soal 18', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(89, 18, 'Opsi 4 untuk soal 18', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(90, 18, 'Opsi 5 untuk soal 18', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(91, 19, 'Opsi 1 untuk soal 19', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(92, 19, 'Opsi 2 untuk soal 19', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(93, 19, 'Opsi 3 untuk soal 19', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(94, 19, 'Opsi 4 untuk soal 19', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(95, 19, 'Opsi 5 untuk soal 19', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(96, 20, 'Opsi 1 untuk soal 20', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(97, 20, 'Opsi 2 untuk soal 20', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(98, 20, 'Opsi 3 untuk soal 20', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(99, 20, 'Opsi 4 untuk soal 20', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(100, 20, 'Opsi 5 untuk soal 20', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(101, 21, 'Opsi 1 untuk soal 21', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(102, 21, 'Opsi 2 untuk soal 21', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(103, 21, 'Opsi 3 untuk soal 21', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(104, 21, 'Opsi 4 untuk soal 21', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(105, 21, 'Opsi 5 untuk soal 21', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(106, 22, 'Opsi 1 untuk soal 22', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(107, 22, 'Opsi 2 untuk soal 22', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(108, 22, 'Opsi 3 untuk soal 22', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(109, 22, 'Opsi 4 untuk soal 22', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(110, 22, 'Opsi 5 untuk soal 22', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(111, 23, 'Opsi 1 untuk soal 23', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(112, 23, 'Opsi 2 untuk soal 23', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(113, 23, 'Opsi 3 untuk soal 23', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(114, 23, 'Opsi 4 untuk soal 23', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(115, 23, 'Opsi 5 untuk soal 23', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(116, 24, 'Opsi 1 untuk soal 24', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(117, 24, 'Opsi 2 untuk soal 24', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(118, 24, 'Opsi 3 untuk soal 24', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(119, 24, 'Opsi 4 untuk soal 24', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(120, 24, 'Opsi 5 untuk soal 24', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(121, 25, 'Opsi 1 untuk soal 25', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(122, 25, 'Opsi 2 untuk soal 25', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(123, 25, 'Opsi 3 untuk soal 25', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(124, 25, 'Opsi 4 untuk soal 25', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(125, 25, 'Opsi 5 untuk soal 25', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(126, 26, 'Opsi 1 untuk soal 26', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(127, 26, 'Opsi 2 untuk soal 26', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(128, 26, 'Opsi 3 untuk soal 26', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(129, 26, 'Opsi 4 untuk soal 26', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(130, 26, 'Opsi 5 untuk soal 26', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(131, 27, 'Opsi 1 untuk soal 27', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(132, 27, 'Opsi 2 untuk soal 27', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(133, 27, 'Opsi 3 untuk soal 27', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(134, 27, 'Opsi 4 untuk soal 27', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(135, 27, 'Opsi 5 untuk soal 27', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(136, 28, 'Opsi 1 untuk soal 28', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(137, 28, 'Opsi 2 untuk soal 28', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(138, 28, 'Opsi 3 untuk soal 28', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(139, 28, 'Opsi 4 untuk soal 28', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(140, 28, 'Opsi 5 untuk soal 28', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(141, 29, 'Opsi 1 untuk soal 29', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(142, 29, 'Opsi 2 untuk soal 29', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(143, 29, 'Opsi 3 untuk soal 29', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(144, 29, 'Opsi 4 untuk soal 29', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(145, 29, 'Opsi 5 untuk soal 29', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(146, 30, 'Opsi 1 untuk soal 30', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(147, 30, 'Opsi 2 untuk soal 30', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(148, 30, 'Opsi 3 untuk soal 30', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(149, 30, 'Opsi 4 untuk soal 30', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(150, 30, 'Opsi 5 untuk soal 30', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(151, 31, 'Opsi 1 untuk soal 31', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(152, 31, 'Opsi 2 untuk soal 31', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(153, 31, 'Opsi 3 untuk soal 31', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(154, 31, 'Opsi 4 untuk soal 31', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(155, 31, 'Opsi 5 untuk soal 31', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(156, 32, 'Opsi 1 untuk soal 32', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(157, 32, 'Opsi 2 untuk soal 32', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(158, 32, 'Opsi 3 untuk soal 32', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(159, 32, 'Opsi 4 untuk soal 32', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(160, 32, 'Opsi 5 untuk soal 32', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(161, 33, 'Opsi 1 untuk soal 33', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(162, 33, 'Opsi 2 untuk soal 33', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(163, 33, 'Opsi 3 untuk soal 33', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(164, 33, 'Opsi 4 untuk soal 33', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(165, 33, 'Opsi 5 untuk soal 33', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(166, 34, 'Opsi 1 untuk soal 34', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(167, 34, 'Opsi 2 untuk soal 34', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(168, 34, 'Opsi 3 untuk soal 34', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(169, 34, 'Opsi 4 untuk soal 34', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(170, 34, 'Opsi 5 untuk soal 34', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(171, 35, 'Opsi 1 untuk soal 35', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(172, 35, 'Opsi 2 untuk soal 35', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(173, 35, 'Opsi 3 untuk soal 35', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(174, 35, 'Opsi 4 untuk soal 35', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(175, 35, 'Opsi 5 untuk soal 35', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(176, 36, 'Opsi 1 untuk soal 36', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(177, 36, 'Opsi 2 untuk soal 36', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(178, 36, 'Opsi 3 untuk soal 36', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(179, 36, 'Opsi 4 untuk soal 36', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(180, 36, 'Opsi 5 untuk soal 36', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(181, 37, 'Opsi 1 untuk soal 37', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(182, 37, 'Opsi 2 untuk soal 37', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(183, 37, 'Opsi 3 untuk soal 37', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(184, 37, 'Opsi 4 untuk soal 37', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(185, 37, 'Opsi 5 untuk soal 37', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(186, 38, 'Opsi 1 untuk soal 38', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(187, 38, 'Opsi 2 untuk soal 38', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(188, 38, 'Opsi 3 untuk soal 38', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(189, 38, 'Opsi 4 untuk soal 38', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(190, 38, 'Opsi 5 untuk soal 38', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(191, 39, 'Opsi 1 untuk soal 39', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(192, 39, 'Opsi 2 untuk soal 39', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(193, 39, 'Opsi 3 untuk soal 39', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(194, 39, 'Opsi 4 untuk soal 39', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(195, 39, 'Opsi 5 untuk soal 39', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(196, 40, 'Opsi 1 untuk soal 40', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(197, 40, 'Opsi 2 untuk soal 40', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(198, 40, 'Opsi 3 untuk soal 40', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(199, 40, 'Opsi 4 untuk soal 40', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(200, 40, 'Opsi 5 untuk soal 40', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(201, 41, 'Opsi 1 untuk soal 41', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(202, 41, 'Opsi 2 untuk soal 41', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(203, 41, 'Opsi 3 untuk soal 41', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(204, 41, 'Opsi 4 untuk soal 41', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(205, 41, 'Opsi 5 untuk soal 41', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(206, 42, 'Opsi 1 untuk soal 42', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(207, 42, 'Opsi 2 untuk soal 42', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(208, 42, 'Opsi 3 untuk soal 42', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(209, 42, 'Opsi 4 untuk soal 42', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(210, 42, 'Opsi 5 untuk soal 42', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(211, 43, 'Opsi 1 untuk soal 43', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(212, 43, 'Opsi 2 untuk soal 43', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(213, 43, 'Opsi 3 untuk soal 43', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(214, 43, 'Opsi 4 untuk soal 43', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(215, 43, 'Opsi 5 untuk soal 43', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(216, 44, 'Opsi 1 untuk soal 44', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(217, 44, 'Opsi 2 untuk soal 44', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(218, 44, 'Opsi 3 untuk soal 44', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(219, 44, 'Opsi 4 untuk soal 44', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(220, 44, 'Opsi 5 untuk soal 44', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(221, 45, 'Opsi 1 untuk soal 45', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(222, 45, 'Opsi 2 untuk soal 45', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(223, 45, 'Opsi 3 untuk soal 45', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(224, 45, 'Opsi 4 untuk soal 45', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(225, 45, 'Opsi 5 untuk soal 45', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(226, 46, 'Opsi 1 untuk soal 46', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(227, 46, 'Opsi 2 untuk soal 46', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(228, 46, 'Opsi 3 untuk soal 46', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(229, 46, 'Opsi 4 untuk soal 46', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(230, 46, 'Opsi 5 untuk soal 46', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(231, 47, 'Opsi 1 untuk soal 47', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(232, 47, 'Opsi 2 untuk soal 47', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(233, 47, 'Opsi 3 untuk soal 47', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(234, 47, 'Opsi 4 untuk soal 47', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(235, 47, 'Opsi 5 untuk soal 47', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(236, 48, 'Opsi 1 untuk soal 48', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(237, 48, 'Opsi 2 untuk soal 48', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(238, 48, 'Opsi 3 untuk soal 48', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(239, 48, 'Opsi 4 untuk soal 48', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(240, 48, 'Opsi 5 untuk soal 48', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(241, 49, 'Opsi 1 untuk soal 49', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(242, 49, 'Opsi 2 untuk soal 49', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(243, 49, 'Opsi 3 untuk soal 49', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(244, 49, 'Opsi 4 untuk soal 49', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(245, 49, 'Opsi 5 untuk soal 49', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(246, 50, 'Opsi 1 untuk soal 50', 2, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(247, 50, 'Opsi 2 untuk soal 50', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(248, 50, 'Opsi 3 untuk soal 50', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(249, 50, 'Opsi 4 untuk soal 50', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(250, 50, 'Opsi 5 untuk soal 50', 0, NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36');

-- --------------------------------------------------------

--
-- Table structure for table `t_prodi`
--

CREATE TABLE `t_prodi` (
  `prodi_id` bigint UNSIGNED NOT NULL,
  `kode_prodi` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `nama` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `jenjang` enum('D2','D3','D4') COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_prodi`
--

INSERT INTO `t_prodi` (`prodi_id`, `kode_prodi`, `nama`, `jenjang`, `created_at`, `updated_at`) VALUES
(1, 'TI', 'Teknik Informatika', 'D4', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(2, 'SI', 'Sistem Informasi', 'D4', '2026-05-07 02:16:36', '2026-05-07 02:16:36');

-- --------------------------------------------------------

--
-- Table structure for table `t_soal`
--

CREATE TABLE `t_soal` (
  `soal_id` bigint UNSIGNED NOT NULL,
  `ujian_id` bigint UNSIGNED NOT NULL,
  `nomer_soal` int NOT NULL,
  `pertanyaan` longtext COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_soal`
--

INSERT INTO `t_soal` (`soal_id`, `ujian_id`, `nomer_soal`, `pertanyaan`, `created_at`, `updated_at`) VALUES
(1, 1, 1, 'Ini adalah pertanyaan ke-1', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(2, 1, 2, 'Ini adalah pertanyaan ke-2', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(3, 1, 3, 'Ini adalah pertanyaan ke-3', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(4, 1, 4, 'Ini adalah pertanyaan ke-4', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(5, 1, 5, 'Ini adalah pertanyaan ke-5', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(6, 1, 6, 'Ini adalah pertanyaan ke-6', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(7, 1, 7, 'Ini adalah pertanyaan ke-7', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(8, 1, 8, 'Ini adalah pertanyaan ke-8', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(9, 1, 9, 'Ini adalah pertanyaan ke-9', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(10, 1, 10, 'Ini adalah pertanyaan ke-10', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(11, 1, 11, 'Ini adalah pertanyaan ke-11', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(12, 1, 12, 'Ini adalah pertanyaan ke-12', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(13, 1, 13, 'Ini adalah pertanyaan ke-13', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(14, 1, 14, 'Ini adalah pertanyaan ke-14', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(15, 1, 15, 'Ini adalah pertanyaan ke-15', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(16, 1, 16, 'Ini adalah pertanyaan ke-16', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(17, 1, 17, 'Ini adalah pertanyaan ke-17', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(18, 1, 18, 'Ini adalah pertanyaan ke-18', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(19, 1, 19, 'Ini adalah pertanyaan ke-19', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(20, 1, 20, 'Ini adalah pertanyaan ke-20', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(21, 1, 21, 'Ini adalah pertanyaan ke-21', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(22, 1, 22, 'Ini adalah pertanyaan ke-22', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(23, 1, 23, 'Ini adalah pertanyaan ke-23', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(24, 1, 24, 'Ini adalah pertanyaan ke-24', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(25, 1, 25, 'Ini adalah pertanyaan ke-25', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(26, 1, 26, 'Ini adalah pertanyaan ke-26', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(27, 1, 27, 'Ini adalah pertanyaan ke-27', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(28, 1, 28, 'Ini adalah pertanyaan ke-28', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(29, 1, 29, 'Ini adalah pertanyaan ke-29', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(30, 1, 30, 'Ini adalah pertanyaan ke-30', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(31, 1, 31, 'Ini adalah pertanyaan ke-31', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(32, 1, 32, 'Ini adalah pertanyaan ke-32', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(33, 1, 33, 'Ini adalah pertanyaan ke-33', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(34, 1, 34, 'Ini adalah pertanyaan ke-34', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(35, 1, 35, 'Ini adalah pertanyaan ke-35', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(36, 1, 36, 'Ini adalah pertanyaan ke-36', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(37, 1, 37, 'Ini adalah pertanyaan ke-37', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(38, 1, 38, 'Ini adalah pertanyaan ke-38', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(39, 1, 39, 'Ini adalah pertanyaan ke-39', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(40, 1, 40, 'Ini adalah pertanyaan ke-40', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(41, 1, 41, 'Ini adalah pertanyaan ke-41', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(42, 1, 42, 'Ini adalah pertanyaan ke-42', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(43, 1, 43, 'Ini adalah pertanyaan ke-43', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(44, 1, 44, 'Ini adalah pertanyaan ke-44', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(45, 1, 45, 'Ini adalah pertanyaan ke-45', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(46, 1, 46, 'Ini adalah pertanyaan ke-46', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(47, 1, 47, 'Ini adalah pertanyaan ke-47', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(48, 1, 48, 'Ini adalah pertanyaan ke-48', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(49, 1, 49, 'Ini adalah pertanyaan ke-49', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(50, 1, 50, 'Ini adalah pertanyaan ke-50', '2026-05-07 02:16:36', '2026-05-07 02:16:36');

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

-- --------------------------------------------------------

--
-- Table structure for table `t_tahun_ajaran`
--

CREATE TABLE `t_tahun_ajaran` (
  `tahun_ajaran_id` bigint UNSIGNED NOT NULL,
  `tahun` year NOT NULL,
  `semester` enum('ganjil','genap') COLLATE utf8mb4_unicode_ci NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_tahun_ajaran`
--

INSERT INTO `t_tahun_ajaran` (`tahun_ajaran_id`, `tahun`, `semester`, `created_at`, `updated_at`) VALUES
(1, '2023', 'ganjil', '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(2, '2024', 'genap', '2026-05-07 02:16:36', '2026-05-07 02:16:36');

-- --------------------------------------------------------

--
-- Table structure for table `t_ujian`
--

CREATE TABLE `t_ujian` (
  `ujian_id` bigint UNSIGNED NOT NULL,
  `matakuliah_id` bigint UNSIGNED NOT NULL,
  `tahun_ajaran_id` bigint UNSIGNED NOT NULL,
  `prodi_id` bigint UNSIGNED NOT NULL,
  `kode_ujian` varchar(30) COLLATE utf8mb4_unicode_ci NOT NULL,
  `nama_ujian` varchar(50) COLLATE utf8mb4_unicode_ci NOT NULL,
  `status` enum('menunggu','dimulai','selesai') COLLATE utf8mb4_unicode_ci NOT NULL DEFAULT 'menunggu',
  `shufflesoal` int NOT NULL,
  `starttime` datetime NOT NULL,
  `endtime` datetime NOT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_ujian`
--

INSERT INTO `t_ujian` (`ujian_id`, `matakuliah_id`, `tahun_ajaran_id`, `prodi_id`, `kode_ujian`, `nama_ujian`, `status`, `shufflesoal`, `starttime`, `endtime`, `created_at`, `updated_at`) VALUES
(1, 1, 1, 1, 'UTS01', 'Ujian Tengah Semester', 'dimulai', 1, '2026-05-07 09:16:36', '2026-05-07 11:16:36', '2026-05-07 02:16:36', '2026-05-07 02:16:36');

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
(1, 1, 1, '2026-05-07 02:16:36', '2026-05-07 02:16:36');

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
  `extendtime` tinyint DEFAULT NULL,
  `tanggal_ujian` datetime DEFAULT NULL,
  `status` enum('menunggu','dimulai','selesai','dihentikan') COLLATE utf8mb4_unicode_ci NOT NULL,
  `keterangan` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `nilai` int DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `t_user`
--

CREATE TABLE `t_user` (
  `user_id` bigint UNSIGNED NOT NULL,
  `dosen_id` bigint UNSIGNED DEFAULT NULL,
  `mahasiswa_id` bigint UNSIGNED DEFAULT NULL,
  `username` varchar(100) COLLATE utf8mb4_unicode_ci NOT NULL,
  `password` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `level` enum('mahasiswa','dosen','panitia') COLLATE utf8mb4_unicode_ci NOT NULL,
  `foto` varchar(255) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
  `created_at` timestamp NULL DEFAULT NULL,
  `updated_at` timestamp NULL DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Dumping data for table `t_user`
--

INSERT INTO `t_user` (`user_id`, `dosen_id`, `mahasiswa_id`, `username`, `password`, `level`, `foto`, `created_at`, `updated_at`) VALUES
(1, 1, NULL, '2241760003', '$2y$12$p8Zq7zI8PfpGwEBmSNWvE.BSW030OWE.MoZR/ffZkDevoKZhqG1fK', 'dosen', NULL, '2026-05-07 02:16:36', '2026-05-07 02:16:36'),
(2, NULL, 1, '2241760001', '$2y$12$s8mRn9.oj530e5CFMBWh5e54PG4xAEnUHaqIwE2CaX0XFAqXgCb2u', 'mahasiswa', NULL, '2026-05-07 02:16:37', '2026-05-07 02:16:37');

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

CREATE TABLE `users` (
  `id` bigint UNSIGNED NOT NULL,
  `name` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `email` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `email_verified_at` timestamp NULL DEFAULT NULL,
  `password` varchar(255) COLLATE utf8mb4_unicode_ci NOT NULL,
  `remember_token` varchar(100) COLLATE utf8mb4_unicode_ci DEFAULT NULL,
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
-- Indexes for table `personal_access_tokens`
--
ALTER TABLE `personal_access_tokens`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `personal_access_tokens_token_unique` (`token`),
  ADD KEY `personal_access_tokens_tokenable_type_tokenable_id_index` (`tokenable_type`,`tokenable_id`),
  ADD KEY `personal_access_tokens_expires_at_index` (`expires_at`);

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
-- Indexes for table `t_kelas_mahasiswa`
--
ALTER TABLE `t_kelas_mahasiswa`
  ADD PRIMARY KEY (`kelas_mahasiswa_id`),
  ADD KEY `t_kelas_mahasiswa_mahasiswa_id_foreign` (`mahasiswa_id`),
  ADD KEY `t_kelas_mahasiswa_kelas_id_foreign` (`kelas_id`);

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
  ADD PRIMARY KEY (`mahasiswa_id`);

--
-- Indexes for table `t_matakuliah`
--
ALTER TABLE `t_matakuliah`
  ADD PRIMARY KEY (`matakuliah_id`),
  ADD UNIQUE KEY `t_matakuliah_kode_matakuliah_unique` (`kode_matakuliah`),
  ADD KEY `t_matakuliah_tahun_ajaran_id_foreign` (`tahun_ajaran_id`),
  ADD KEY `t_matakuliah_prodi_id_foreign` (`prodi_id`);

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
  MODIFY `id` int UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=36;

--
-- AUTO_INCREMENT for table `personal_access_tokens`
--
ALTER TABLE `personal_access_tokens`
  MODIFY `id` bigint UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `t_dosen`
--
ALTER TABLE `t_dosen`
  MODIFY `dosen_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT for table `t_gambar_soal`
--
ALTER TABLE `t_gambar_soal`
  MODIFY `gambar_soal_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT for table `t_kelas`
--
ALTER TABLE `t_kelas`
  MODIFY `kelas_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT for table `t_kelas_mahasiswa`
--
ALTER TABLE `t_kelas_mahasiswa`
  MODIFY `kelas_mahasiswa_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT for table `t_kelas_matakuliah`
--
ALTER TABLE `t_kelas_matakuliah`
  MODIFY `kelas_matakuliah_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT for table `t_mahasiswa`
--
ALTER TABLE `t_mahasiswa`
  MODIFY `mahasiswa_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT for table `t_matakuliah`
--
ALTER TABLE `t_matakuliah`
  MODIFY `matakuliah_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT for table `t_opsi_jawaban`
--
ALTER TABLE `t_opsi_jawaban`
  MODIFY `opsi_jawaban_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=251;

--
-- AUTO_INCREMENT for table `t_prodi`
--
ALTER TABLE `t_prodi`
  MODIFY `prodi_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT for table `t_soal`
--
ALTER TABLE `t_soal`
  MODIFY `soal_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=51;

--
-- AUTO_INCREMENT for table `t_soal_mahasiswa`
--
ALTER TABLE `t_soal_mahasiswa`
  MODIFY `soal_mahasiswa_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `t_tahun_ajaran`
--
ALTER TABLE `t_tahun_ajaran`
  MODIFY `tahun_ajaran_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT for table `t_ujian`
--
ALTER TABLE `t_ujian`
  MODIFY `ujian_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT for table `t_ujian_kelas`
--
ALTER TABLE `t_ujian_kelas`
  MODIFY `ujian_kelas_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=2;

--
-- AUTO_INCREMENT for table `t_ujian_mahasiswa`
--
ALTER TABLE `t_ujian_mahasiswa`
  MODIFY `ujianmahasiswa_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `t_user`
--
ALTER TABLE `t_user`
  MODIFY `user_id` bigint UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

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
  ADD CONSTRAINT `t_kelas_prodi_id_foreign` FOREIGN KEY (`prodi_id`) REFERENCES `t_prodi` (`prodi_id`) ON DELETE RESTRICT,
  ADD CONSTRAINT `t_kelas_tahun_ajaran_id_foreign` FOREIGN KEY (`tahun_ajaran_id`) REFERENCES `t_tahun_ajaran` (`tahun_ajaran_id`) ON DELETE RESTRICT;

--
-- Constraints for table `t_kelas_mahasiswa`
--
ALTER TABLE `t_kelas_mahasiswa`
  ADD CONSTRAINT `t_kelas_mahasiswa_kelas_id_foreign` FOREIGN KEY (`kelas_id`) REFERENCES `t_kelas` (`kelas_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `t_kelas_mahasiswa_mahasiswa_id_foreign` FOREIGN KEY (`mahasiswa_id`) REFERENCES `t_mahasiswa` (`mahasiswa_id`) ON DELETE CASCADE;

--
-- Constraints for table `t_kelas_matakuliah`
--
ALTER TABLE `t_kelas_matakuliah`
  ADD CONSTRAINT `t_kelas_matakuliah_dosen_id_foreign` FOREIGN KEY (`dosen_id`) REFERENCES `t_dosen` (`dosen_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `t_kelas_matakuliah_kelas_id_foreign` FOREIGN KEY (`kelas_id`) REFERENCES `t_kelas` (`kelas_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `t_kelas_matakuliah_matakuliah_id_foreign` FOREIGN KEY (`matakuliah_id`) REFERENCES `t_matakuliah` (`matakuliah_id`) ON DELETE CASCADE;

--
-- Constraints for table `t_matakuliah`
--
ALTER TABLE `t_matakuliah`
  ADD CONSTRAINT `t_matakuliah_prodi_id_foreign` FOREIGN KEY (`prodi_id`) REFERENCES `t_prodi` (`prodi_id`) ON DELETE RESTRICT,
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
