using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace NPCCustomization.Editor
{
    /// <summary>
    /// Editor Window untuk auto-import sprites dan generate NPCPartData assets.
    /// Menghemat BANYAK waktu dengan auto-detect animation states dan grouping sprites.
    /// </summary>
    public class NPCPartImporterWindow : EditorWindow
    {
        private string sourceFolderPath = "Assets/Farm RPG - Tiny Asset Pack - (All in One)/Character and Portrait/Character/PNG";
        private string outputFolderPath = "Assets/NPCParts/Generated";
        
        private Vector2 scrollPosition;
        private bool showAdvancedOptions = false;
        
        [MenuItem("Window/NPC Customization/Part Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<NPCPartImporterWindow>("NPC Part Importer");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("NPC Part Importer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Tool ini akan scan folder sprites dan auto-generate NPCPartData assets. Menghemat waktu manual assignment ratusan sprites!", MessageType.Info);

            GUILayout.Space(10);

            // Source folder
            EditorGUILayout.LabelField("Source Folder:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            sourceFolderPath = EditorGUILayout.TextField(sourceFolderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Source Folder", "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    sourceFolderPath = FileUtil.GetProjectRelativePath(selected);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Output folder
            EditorGUILayout.LabelField("Output Folder:", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            outputFolderPath = EditorGUILayout.TextField(outputFolderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    outputFolderPath = FileUtil.GetProjectRelativePath(selected);
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Advanced options
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
            if (showAdvancedOptions)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("Coming soon: custom classification rules, sprite sheet slicing options, etc.", MessageType.Info);
                EditorGUI.indentLevel--;
            }

            GUILayout.Space(20);

            // Import button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Import All Parts", GUILayout.Height(40)))
            {
                ImportAllParts();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            // Quick actions
            EditorGUILayout.LabelField("Quick Actions:", EditorStyles.boldLabel);
            if (GUILayout.Button("Import Skins Only"))
            {
                ImportCategory("Skins", PartCategory.Skin);
            }
            if (GUILayout.Button("Import Hair Only"))
            {
                ImportCategory("Hair's", PartCategory.Hair);
            }
            if (GUILayout.Button("Import Eyes Only"))
            {
                ImportCategory("Eyes", PartCategory.Eyes);
            }
            if (GUILayout.Button("Import Clothes Only"))
            {
                ImportCategory("Clothers", PartCategory.Clothes);
            }
            if (GUILayout.Button("Import Accessories Only"))
            {
                ImportCategory("Acc", PartCategory.Accessory);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Import all parts dari source folder
        /// </summary>
        private void ImportAllParts()
        {
            if (!Directory.Exists(sourceFolderPath))
            {
                EditorUtility.DisplayDialog("Error", $"Source folder tidak ditemukan: {sourceFolderPath}", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Importing Parts", "Starting import...", 0f);

            try
            {
                // Get all animation state folders (1. Idle, 2. Walk, dll)
                string[] animationFolders = Directory.GetDirectories(sourceFolderPath);
                
                if (animationFolders.Length == 0)
                {
                    EditorUtility.DisplayDialog("Error", "Tidak ada animation folders ditemukan!", "OK");
                    return;
                }

                // Detect categories dari folder pertama (Idle)
                string firstAnimFolder = animationFolders[0];
                string[] categoryFolders = Directory.GetDirectories(firstAnimFolder);

                int totalCategories = categoryFolders.Length;
                int currentCategory = 0;

                foreach (string categoryFolder in categoryFolders)
                {
                    string categoryName = Path.GetFileName(categoryFolder);
                    float progress = (float)currentCategory / totalCategories;
                    EditorUtility.DisplayProgressBar("Importing Parts", $"Processing {categoryName}...", progress);

                    ImportCategoryFromFolder(categoryName, categoryFolder);

                    currentCategory++;
                }

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Success", $"Import selesai! Assets saved to: {outputFolderPath}", "OK");
                
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Import error: {e.Message}", "OK");
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Import specific category
        /// </summary>
        private void ImportCategory(string categoryFolderName, PartCategory category)
        {
            EditorUtility.DisplayProgressBar("Importing Category", $"Processing {categoryFolderName}...", 0f);

            try
            {
                string categoryPath = Path.Combine(sourceFolderPath, "1. Idle", categoryFolderName);
                ImportCategoryFromFolder(categoryFolderName, categoryPath);

                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Success", $"{categoryFolderName} imported successfully!", "OK");
                
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Import error: {e.Message}", "OK");
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// Import category dari specific folder
        /// </summary>
        private void ImportCategoryFromFolder(string categoryFolderName, string categoryPath)
        {
            PartCategory category = DetermineCategory(categoryFolderName);

            // Detect parts dalam category ini
            // Bisa berupa files langsung, atau subfolder dengan variants
            
            if (Directory.Exists(categoryPath))
            {
                // Check apakah ada subfolders (variants)
                string[] subfolders = Directory.GetDirectories(categoryPath);
                
                // Process subfolders (misal: Hair's -> Fawn, Iridessa, dll)
                foreach (string subfolder in subfolders)
                {
                    string partName = Path.GetFileName(subfolder);
                    ProcessPart(partName, subfolder, category);
                }
                
                // ALSO process direct files in this folder (misal: Acc -> Beret.png, Wizard.png)
                // This handles mixed structure (folders + direct files)
                string[] files = Directory.GetFiles(categoryPath, "*.png");
                
                // Group by base name (tanpa extension)
                var groupedFiles = files.GroupBy(f => Path.GetFileNameWithoutExtension(f));
                
                foreach (var group in groupedFiles)
                {
                    string partName = group.Key;
                    // Skip .meta files
                    if (!partName.EndsWith(".meta"))
                    {
                        ProcessPartFromFiles(partName, categoryPath, category);
                    }
                }
            }
        }

        /// <summary>
        /// Process single part dan buat NPCPartData
        /// UPDATED: Handle nested variants (Fawn/Black.png, Farm/Blue.png, Female/1.png)
        /// </summary>
        private void ProcessPart(string partName, string partFolder, PartCategory category)
        {
            // Check apakah partFolder punya subfolders dengan variants
            string[] variantFolders = Directory.GetDirectories(partFolder);
            
            if (variantFolders.Length > 0)
            {
                // Ada nested variants (misal: Fawn/ punya Black.png, Blonde.png, Brown.png)
                foreach (string variantFolder in variantFolders)
                {
                    string variantName = Path.GetFileName(variantFolder);
                    ProcessVariant(partName, variantName, variantFolder, category);
                }
            }
            else
            {
                // Tidak ada nested variants, check PNG files langsung
                string[] pngFiles = Directory.GetFiles(partFolder, "*.png");
                
                if (pngFiles.Length > 0)
                {
                    // Process EACH PNG file sebagai asset terpisah
                    // (misal: Female/Black.png, Female/Blue.png -> Female_Black.asset, Female_Blue.asset)
                    foreach (string pngFile in pngFiles)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(pngFile);
                        
                        // Skip meta files
                        if (fileName.EndsWith(".meta")) continue;
                        
                        // Process this file as individual variant
                        ProcessIndividualFile(partName, fileName, partFolder, category);
                    }
                }
            }
        }

        /// <summary>
        /// Process individual PNG file di folder (untuk Eyes, Skins yang punya direct files)
        /// </summary>
        private void ProcessIndividualFile(string partStyleName, string fileName, string partFolder, PartCategory category)
        {
            // Collect sprites untuk file ini dari semua animation states
            Dictionary<string, Sprite[]> animationSprites = new Dictionary<string, Sprite[]>();

            // Get all animation folders
            string[] animationFolders = Directory.GetDirectories(sourceFolderPath);
            
            // Get category folder name
            string categoryFolderName = Path.GetFileName(Directory.GetParent(partFolder).FullName);

            foreach (string animFolder in animationFolders)
            {
                string animName = Path.GetFileName(animFolder);
                
                // Build path: animFolder/CategoryFolder/StyleFolder/FileName.png
                string filePathInAnim = Path.Combine(animFolder, categoryFolderName, partStyleName, fileName + ".png");

                if (File.Exists(filePathInAnim))
                {
                    // Load ALL sprites dari sprite sheet (Multiple mode)
                    UnityEngine.Object[] allSprites = AssetDatabase.LoadAllAssetsAtPath(filePathInAnim);
                    
                    // Filter hanya Sprite objects
                    List<Sprite> sprites = new List<Sprite>();
                    foreach (var obj in allSprites)
                    {
                        if (obj is Sprite sprite)
                        {
                            sprites.Add(sprite);
                        }
                    }
                    
                    if (sprites.Count > 0)
                    {
                        // Natural sort (numeric aware) untuk urutan frame yang benar
                        sprites.Sort((a, b) => NaturalCompare(a.name, b.name));
                        animationSprites[animName] = sprites.ToArray();
                    }
                }
            }

            // Create NPCPartData asset dengan nama: StyleName_FileName
            if (animationSprites.Count > 0)
            {
                string fullPartName = $"{partStyleName}_{fileName}";
                CreateNPCPartDataAsset(fullPartName, category, animationSprites);
            }
            else
            {
                Debug.LogWarning($"No sprites collected for {partStyleName}/{fileName}");
            }
        }

        /// <summary>
        /// Process variant dalam nested folder (misal: Fawn/Black.png, Fawn/Blonde.png)
        /// FIXED: Load ALL sprites from sprite sheet (multiple mode)
        /// </summary>
        private void ProcessVariant(string partStyleName, string variantName, string variantFolder, PartCategory category)
        {
            // Get all PNG files di variant folder ini
            string[] variantFiles = Directory.GetFiles(variantFolder, "*.png");
            
            if (variantFiles.Length == 0)
            {
                Debug.LogWarning($"No PNG files found in {variantFolder}");
                return;
            }

            // Process each PNG file sebagai variant terpisah
            foreach (string variantFile in variantFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(variantFile);
                
                // Skip meta files
                if (fileName.EndsWith(".meta")) continue;

                // Collect sprites untuk file ini dari semua animation states
                Dictionary<string, Sprite[]> animationSprites = new Dictionary<string, Sprite[]>();

                // Get all animation folders
                string[] animationFolders = Directory.GetDirectories(sourceFolderPath);

                foreach (string animFolder in animationFolders)
                {
                    string animName = Path.GetFileName(animFolder);
                    
                    // Get category folder name
                    string categoryFolderName = Path.GetFileName(Directory.GetParent(variantFolder).Parent.FullName);
                    
                    // Build path: animFolder/CategoryFolder/StyleFolder/FileName.png
                    string filePathInAnim = Path.Combine(animFolder, categoryFolderName, partStyleName, fileName + ".png");

                    if (File.Exists(filePathInAnim))
                    {
                        // Load ALL sprites dari sprite sheet (Multiple mode)
                        UnityEngine.Object[] allSprites = AssetDatabase.LoadAllAssetsAtPath(filePathInAnim);
                        
                        // Filter hanya Sprite objects
                        List<Sprite> sprites = new List<Sprite>();
                        foreach (var obj in allSprites)
                        {
                            if (obj is Sprite sprite)
                            {
                                sprites.Add(sprite);
                            }
                        }
                        
                        if (sprites.Count > 0)
                        {
                            // Natural sort (numeric aware) untuk urutan frame yang benar
                            sprites.Sort((a, b) => NaturalCompare(a.name, b.name));
                            animationSprites[animName] = sprites.ToArray();
                        }
                    }
                    else
                    {
                        // Debug log jika file tidak ditemukan
                        // Debug.Log($"File not found: {filePathInAnim}");
                    }
                }

                // Create NPCPartData asset dengan nama: StyleName_FileName
                if (animationSprites.Count > 0)
                {
                    string fullPartName = $"{partStyleName}_{fileName}";
                    CreateNPCPartDataAsset(fullPartName, category, animationSprites);
                }
                else
                {
                    Debug.LogWarning($"No sprites collected for {partStyleName}/{fileName}");
                }
            }
        }

        /// <summary>
        /// Process single part tanpa nested variants
        /// </summary>
        private void ProcessSinglePart(string partName, string partFolder, PartCategory category)
        {
            // Collect sprites dari semua animation states
            Dictionary<string, Sprite[]> animationSprites = new Dictionary<string, Sprite[]>();

            // Get all animation folders
            string[] animationFolders = Directory.GetDirectories(sourceFolderPath);

            foreach (string animFolder in animationFolders)
            {
                string animName = Path.GetFileName(animFolder);
                
                // Path ke part dalam animation folder ini
                string partPathInAnim = Path.Combine(animFolder, Path.GetFileName(Path.GetDirectoryName(partFolder)), partName);

                if (Directory.Exists(partPathInAnim))
                {
                    // Load sprites dari folder ini
                    Sprite[] sprites = LoadSpritesFromFolder(partPathInAnim);
                    
                    if (sprites.Length > 0)
                    {
                        animationSprites[animName] = sprites;
                    }
                }
            }

            // Create NPCPartData asset
            if (animationSprites.Count > 0)
            {
                CreateNPCPartDataAsset(partName, category, animationSprites);
            }
        }

        /// <summary>
        /// Process part dari direct files (untuk accessories)
        /// </summary>
        private void ProcessPartFromFiles(string partName, string categoryPath, PartCategory category)
        {
            // Similar logic tapi untuk direct files
            Dictionary<string, Sprite[]> animationSprites = new Dictionary<string, Sprite[]>();

            string[] animationFolders = Directory.GetDirectories(sourceFolderPath);

            foreach (string animFolder in animationFolders)
            {
                string animName = Path.GetFileName(animFolder);
                string categoryFolderName = Path.GetFileName(categoryPath);
                
                string partPathInAnim = Path.Combine(animFolder, categoryFolderName, partName + ".png");

                if (File.Exists(partPathInAnim))
                {
                    // Load ALL sprites dari sprite sheet (Multiple mode)
                    UnityEngine.Object[] allSprites = AssetDatabase.LoadAllAssetsAtPath(partPathInAnim);
                    
                    // Filter hanya Sprite objects
                    List<Sprite> sprites = new List<Sprite>();
                    foreach (var obj in allSprites)
                    {
                        if (obj is Sprite sprite)
                        {
                            sprites.Add(sprite);
                        }
                    }
                    
                    if (sprites.Count > 0)
                    {
                        // Natural sort (numeric aware) untuk urutan frame yang benar
                        sprites.Sort((a, b) => NaturalCompare(a.name, b.name));
                        animationSprites[animName] = sprites.ToArray();
                    }
                }
            }

            if (animationSprites.Count > 0)
            {
                CreateNPCPartDataAsset(partName, category, animationSprites);
            }
        }

        /// <summary>
        /// Load sprites dari folder
        /// </summary>
        private Sprite[] LoadSpritesFromFolder(string folderPath)
        {
            List<Sprite> sprites = new List<Sprite>();
            
            string[] files = Directory.GetFiles(folderPath, "*.png");
            
            foreach (string file in files)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(file);
                if (sprite != null)
                {
                    sprites.Add(sprite);
                }
            }

            // Natural sort (numeric aware) untuk urutan frame yang benar
            sprites.Sort((a, b) => NaturalCompare(a.name, b.name));

            return sprites.ToArray();
        }

        /// <summary>
        /// Create NPCPartData asset
        /// </summary>
        private void CreateNPCPartDataAsset(string partName, PartCategory category, Dictionary<string, Sprite[]> animationSprites)
        {
            // Create output folder jika belum ada
            string categoryOutputPath = Path.Combine(outputFolderPath, category.ToString());
            
            if (!Directory.Exists(categoryOutputPath))
            {
                Directory.CreateDirectory(categoryOutputPath);
            }

            // Create asset
            NPCPartData partData = ScriptableObject.CreateInstance<NPCPartData>();
            partData.partName = partName;
            partData.category = category;

           // Populate animation states
            List<AnimationStateSprites> statesList = new List<AnimationStateSprites>();
            
            foreach (var kvp in animationSprites)
            {
                AnimationStateSprites state = new AnimationStateSprites(kvp.Key);
                state.frames = kvp.Value;
                statesList.Add(state);
            }

            partData.animationStates = statesList.ToArray();

            // Determine accessory type jika accessory
            if (category == PartCategory.Accessory)
            {
                partData.accessoryType = DetermineAccessoryType(partName);
            }

            // Save asset
            string assetPath = Path.Combine(categoryOutputPath, $"{partName}.asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            
            AssetDatabase.CreateAsset(partData, assetPath);
            
            Debug.Log($"Created: {assetPath}");
        }

        /// <summary>
        /// Determine PartCategory dari folder name
        /// </summary>
        private PartCategory DetermineCategory(string folderName)
        {
            if (folderName.Contains("Skin")) return PartCategory.Skin;
            if (folderName.Contains("Hair")) return PartCategory.Hair;
            if (folderName.Contains("Eye")) return PartCategory.Eyes;
            if (folderName.Contains("Cloth")) return PartCategory.Clothes;
            if (folderName.Contains("Acc")) return PartCategory.Accessory;
            
            return PartCategory.Accessory; // Default
        }

        /// <summary>
        /// Determine AccessoryType (permanent vs switchable)
        /// </summary>
        private AccessoryType DetermineAccessoryType(string partName)
        {
            string nameLower = partName.ToLower();

            // Permanent accessories (bagian dari body yang menyatu)
            if (nameLower.Contains("beard") || 
                nameLower.Contains("elf"))
            {
                return AccessoryType.Permanent;
            }

            // Default: switchable (hats, costumes, dll)
            return AccessoryType.Switchable;
        }

        /// <summary>
        /// Natural comparison untuk sort numeric-aware
        /// Contoh: Black_0, Black_1, Black_2, ..., Black_10, Black_11 (tidak Black_1, Black_10, Black_11, Black_2)
        /// </summary>
        private static int NaturalCompare(string a, string b)
        {
            // Simple natural sort implementation
            int aIndex = 0, bIndex = 0;
            
            while (aIndex < a.Length && bIndex < b.Length)
            {
                // Check if both are digits
                if (char.IsDigit(a[aIndex]) && char.IsDigit(b[bIndex]))
                {
                    // Extract numbers
                    string aNum = "";
                    while (aIndex < a.Length && char.IsDigit(a[aIndex]))
                    {
                        aNum += a[aIndex];
                        aIndex++;
                    }
                    
                    string bNum = "";
                    while (bIndex < b.Length && char.IsDigit(b[bIndex]))
                    {
                        bNum += b[bIndex];
                        bIndex++;
                    }
                    
                    // Compare as numbers
                    int result = int.Parse(aNum).CompareTo(int.Parse(bNum));
                    if (result != 0) return result;
                }
                else
                {
                    // Compare as characters
                    int result = a[aIndex].CompareTo(b[bIndex]);
                    if (result != 0) return result;
                    
                    aIndex++;
                    bIndex++;
                }
            }
            
            // If one string is prefix of another
            return a.Length.CompareTo(b.Length);
        }
    }
}
