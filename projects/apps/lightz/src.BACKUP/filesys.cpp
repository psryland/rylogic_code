#include "filesys.h"
#include "utils/utils.h"

namespace lightz
{
	// Singleton instance of the file system
	FileSys filesys;

	FileSys::FileSys()
	{
		#if 0
		// Initialse the filesystem
		esp_vfs_spiffs_conf_t conf = {
			.base_path = "/root",
			.partition_label = "storage",
			.max_files = 5,
			.format_if_mount_failed = false,
		};
		Check(esp_vfs_spiffs_register(&conf), {
			{{}, "SPIFFS failed to mount"},
		});

		size_t total = 0, used = 0;
		Check(esp_spiffs_info(conf.partition_label, &total, &used), {
			{{}, "Failed to get LittleFS partition information"}
		});
#endif

#if 0
		ESP_LOGI(TAG, "Initializing LittleFS");

		esp_vfs_littlefs_conf_t conf = {
			.base_path = "/root",
			.partition_label = "storage",
			.format_if_mount_failed = true,
			.dont_mount = false,
		};
		Check(esp_vfs_littlefs_register(&conf), {
			{ESP_FAIL, "Failed to mount or format filesystem"},
			{ESP_ERR_NOT_FOUND, "Failed to find LittleFS partition"},
			{{}, "Failed to initialize LittleFS"}
		});

		size_t total = 0, used = 0;
		Check(esp_littlefs_info(conf.partition_label, &total, &used), {
			{{}, "Failed to get LittleFS partition information"}
		});
		ESP_LOGI(TAG, "Partition size: total: %d, used: %d", total, used);
#endif
/*
    // Use POSIX and C standard library functions to work with files.
    // First create a file.
    ESP_LOGI(TAG, "Opening file");
    FILE *f = fopen("/littlefs/hello.txt", "w");
    if (f == NULL) {
        ESP_LOGE(TAG, "Failed to open file for writing");
        return;
    }
    fprintf(f, "Hello World!\n");
    fclose(f);
    ESP_LOGI(TAG, "File written");

    // Check if destination file exists before renaming
    struct stat st;
    if (stat("/littlefs/foo.txt", &st) == 0) {
        // Delete it if it exists
        unlink("/littlefs/foo.txt");
    }

    // Rename original file
    ESP_LOGI(TAG, "Renaming file");
    if (rename("/littlefs/hello.txt", "/littlefs/foo.txt") != 0) {
        ESP_LOGE(TAG, "Rename failed");
        return;
    }

    // Open renamed file for reading
    ESP_LOGI(TAG, "Reading file");
    f = fopen("/littlefs/foo.txt", "r");
    if (f == NULL) {
        ESP_LOGE(TAG, "Failed to open file for reading");
        return;
    }

    char line[128];
    fgets(line, sizeof(line), f);
    fclose(f);
    // strip newline
    char*pos = strchr(line, '\n');
    if (pos) {
        *pos = '\0';
    }
    ESP_LOGI(TAG, "Read from file: '%s'", line);

    ESP_LOGI(TAG, "Reading from flashed filesystem example.txt");
    f = fopen("/littlefs/example.txt", "r");
    if (f == NULL) {
        ESP_LOGE(TAG, "Failed to open file for reading");
        return;
    }
    fgets(line, sizeof(line), f);
    fclose(f);
    // strip newline
    pos = strchr(line, '\n');
    if (pos) {
        *pos = '\0';
    }
    ESP_LOGI(TAG, "Read from file: '%s'", line);

    // All done, unmount partition and disable LittleFS
    esp_vfs_littlefs_unregister(conf.partition_label);
    ESP_LOGI(TAG, "LittleFS unmounted");
	}
*/
	}
/*
	void FormatFS()
	{
		esp_littlefs_format(conf.partition_label);
	}
*/
}





#if 0
#include "filesys.h"

namespace lightz
{
	FileSys::FileSys()
	 	: fs::LittleFSFS()
	{}
	
	void FileSys::Setup()
	{
		if (begin(false, "/root"))
			return;
	
		Serial.println("Failed to mount the file system");

		#if 0
		if (!format())
		{
			Serial.println("Failed to format the file system");
			throw std::runtime_error("Failed to mount the file system");
		}
		#endif

		// Try mount again
		if (!begin(false, "/root"))
		{
			Serial.println("Failed to mount the file system");
			throw std::runtime_error("Failed to mount the file system");
		}
	}
}
#endif
