# AutoPatchPluginCL
AutoPatch Plugin for ConquerLoader

<img width="1024" height="1536" alt="Preview Promo" src="https://github.com/user-attachments/assets/59fb3117-5b5f-42e0-9e46-c8dd0d9c3219" />

<img width="553" height="175" alt="image" src="https://github.com/user-attachments/assets/91281747-cec5-4fab-8d4c-442e1b21716d" />

## Requeriments
- ConquerLoader 1.0.0.9

## How to use
1- Create a PatchList.json in your apache website folder. Example structure here: ![ExampleStructurePatchListJSON](https://i.ibb.co/Lvn71Lv/125979718-3472153672831781-1826451695129960471-n.jpg)\
2- Extract the .zip release to Conquer Client Folder\
3- Execute one time the Loader and close\
4- Modify the AutoPatchPluginCLConfig.json and change the parameter PatchListUrl with your domain or if only is for testing you can use localhost, exemple: http://localhost/PatchList.json
\
5- All ready!

## PathList.json example

```
{
    "CurrentVersion": 3,
    "Paths": [
        {
            "Version": 2,
            "RelativeURL": "patch2.rar"
        },
        {
            "Version": 3,
            "RelativeURL": "patch3.rar"
        },
        {
            "Version": 1,
            "RelativeURL": "patch1.rar"
        }
    ]
}
```

# Enable edit mode
- Go to ConquerLoader > Settings > Plugins > AutoPatchPluginCL

## Compatible patch formats
- Rar
- Zip
- 7-Zip
- GZip
- Tar
