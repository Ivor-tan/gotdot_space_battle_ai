
#if TOOLS
using Godot;

[Tool]
public partial class AssetToolWindow : Window
{
    AssetConfig config;

    ItemList scanPathList;
    LineEdit outputPathEdit;

    Tree tree;

    EditorFileDialog folderDialog;
    EditorFileDialog saveDialog;

    string configPath =
        "res://addons/asset_path_maintenance/AssetConfig.tres";

    string scriptPath =
        "res://addons/asset_path_maintenance/AssetConfig.cs";

    public override void _Ready()
    {
        Title = "Asset Path Maintenance";

        MinSize = new Vector2I(650, 450);

        CloseRequested += () => Hide();

        config = AssetConfigIO.Load();
        if (config == null)
        {
            GD.PrintErr("Failed to load config.");
            return;
        }
        if (config.ScanPaths == null)
            config.ScanPaths = new();

        if (config.Assets == null)
            config.Assets = new();

        BuildUI();

        Refresh();
    }

    //------------------------------------------------
    // UI
    //------------------------------------------------

    void BuildUI()
    {
        VBoxContainer root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        AddChild(root);

        //------------------------------------------------
        // Scan Folder
        //------------------------------------------------

        Label scanLabel = new Label();
        scanLabel.Text = "Scan Folders";

        root.AddChild(scanLabel);

        scanPathList = new ItemList();
        scanPathList.SizeFlagsVertical = Control.SizeFlags.ExpandFill;

        foreach (var p in config.ScanPaths)
        {
            scanPathList.AddItem(p);
        }

        root.AddChild(scanPathList);

        HBoxContainer scanButtons = new HBoxContainer();

        Button addBtn = new Button();
        addBtn.Text = "Add Folder";

        Button removeBtn = new Button();
        removeBtn.Text = "Remove";

        Button scanBtn = new Button();
        scanBtn.Text = "Scan";

        addBtn.Pressed += OnAddFolder;
        removeBtn.Pressed += OnRemoveFolder;
        scanBtn.Pressed += OnScan;

        scanButtons.AddChild(addBtn);
        scanButtons.AddChild(removeBtn);
        scanButtons.AddChild(scanBtn);

        root.AddChild(scanButtons);

        //------------------------------------------------
        // Output Path
        //------------------------------------------------

        HBoxContainer outputBar = new HBoxContainer();

        Label outputLabel = new Label();
        outputLabel.Text = "Output Path";

        outputPathEdit = new LineEdit();
        outputPathEdit.Text = config.OutputPath;
        outputPathEdit.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        Button browseBtn = new Button();
        browseBtn.Text = "Browse";
        browseBtn.Pressed += OnBrowseOutput;

        outputBar.AddChild(outputLabel);
        outputBar.AddChild(outputPathEdit);
        outputBar.AddChild(browseBtn);

        root.AddChild(outputBar);

        //------------------------------------------------
        // Tree
        //------------------------------------------------

        tree = new Tree();

        tree.Columns = 2;

        tree.SetColumnTitle(0, "Name");
        tree.SetColumnTitle(1, "Path");

        tree.SetColumnTitlesVisible(true);

        tree.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        tree.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        root.AddChild(tree);

        //------------------------------------------------
        // Bottom
        //------------------------------------------------

        HBoxContainer bottom = new HBoxContainer();

        Control spacer = new Control();
        spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

        Button generateBtn = new Button();
        generateBtn.Text = "Generate Code";
        generateBtn.Pressed += OnGenerate;

        bottom.AddChild(spacer);
        bottom.AddChild(generateBtn);

        root.AddChild(bottom);

        //------------------------------------------------
        // Folder Dialog
        //------------------------------------------------

        folderDialog = new EditorFileDialog();

        folderDialog.FileMode =
            EditorFileDialog.FileModeEnum.OpenDir;

        folderDialog.Access =
            EditorFileDialog.AccessEnum.Resources;

        folderDialog.DirSelected += OnFolderSelected;

        AddChild(folderDialog);

        //------------------------------------------------
        // Save Dialog
        //------------------------------------------------

        saveDialog = new EditorFileDialog();

        saveDialog.FileMode =
            EditorFileDialog.FileModeEnum.SaveFile;

        saveDialog.Access =
            EditorFileDialog.AccessEnum.Resources;

        saveDialog.Filters = new string[]
        {
            "*.cs ; C# Script"
        };

        saveDialog.FileSelected += OnOutputSelected;

        AddChild(saveDialog);
    }

    //------------------------------------------------
    // Add Folder
    //------------------------------------------------

    void OnAddFolder()
    {
        folderDialog.PopupCentered();
    }

    //------------------------------------------------
    // Folder Selected
    //------------------------------------------------

    void OnFolderSelected(string path)
    {
        if (config.ScanPaths.Contains(path))
            return;

        config.ScanPaths.Add(path);

        scanPathList.AddItem(path);

        Save();
    }

    //------------------------------------------------
    // Remove Folder
    //------------------------------------------------

    void OnRemoveFolder()
    {
        var selected = scanPathList.GetSelectedItems();

        if (selected.Length == 0)
            return;

        int index = selected[0];

        config.ScanPaths.RemoveAt(index);

        scanPathList.RemoveItem(index);

        Save();
    }

    //------------------------------------------------
    // Scan
    //------------------------------------------------

    void OnScan()
    {
        AssetScanner.Scan(config);

        Save();

        Refresh();
    }

    //------------------------------------------------
    // Browse Output
    //------------------------------------------------

    void OnBrowseOutput()
    {
        saveDialog.PopupCentered();
    }

    //------------------------------------------------
    // Output Selected
    //------------------------------------------------

    void OnOutputSelected(string path)
    {
        outputPathEdit.Text = path;

        config.OutputPath = path;

        Save();
    }

    //------------------------------------------------
    // Generate
    //------------------------------------------------

    void OnGenerate()
    {
        config.OutputPath = outputPathEdit.Text;

        Save();

        AssetCodeGenerator.Generate(config);
    }

    //------------------------------------------------
    // Refresh Tree
    //------------------------------------------------

    void Refresh()
    {
        tree.Clear();

        TreeItem root = tree.CreateItem();

        foreach (var entry in config.Assets)
        {
            TreeItem item = tree.CreateItem(root);

            item.SetText(0, entry.Name);
            item.SetText(1, entry.Path);
        }
    }


    //------------------------------------------------
    // Save
    //------------------------------------------------

    void Save()
    {
        AssetConfigIO.Save(config);
    }
}

#endif