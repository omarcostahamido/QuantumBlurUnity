﻿// -*- coding: utf-8 -*-

// This code is part of Qiskit.
//
// (C) Copyright IBM 2020.
//
// This code is licensed under the Apache License, Version 2.0. You may
// obtain a copy of this license in the LICENSE.txt file in the root directory
// of this source tree or at http://www.apache.org/licenses/LICENSE-2.0.
//
// Any modifications or derivative works of this code must retain this
// copyright notice, and modified files need to carry a notice indicating
// that they have been altered from the originals.

using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour {

    [Tooltip("The texture which gets modified with the quantum effect. MUST HAVE READ/WRITE ENABLED! (Choose a small texture (256x256 at first)")]
    public Texture2D TextureToBlur;
    [Tooltip("Used in the blur effect, determines how strong the image gets chag")]
    public float BlurRotation = 0.25f;

    [Tooltip("The texture which gets made into a terrain. Gets generated by Blur effect, but can be set seperately (make sure to have read/write enabled and not to choose a too big texture)")]
    public Texture2D InputTexture;

    [Tooltip("Normally the lighter a pixel is the higher the terrain will be. This behaviour is inverted if checked.")]
    public bool Invert;
    [Tooltip("The maximal height the terrain will have.")]
    public int MaxHeight = 10;
    [Tooltip("The threshold used by the noise methods")]
    public float Threshold = 0.5f;
    [Tooltip("If this is ticked, the most bottom cube will always be set, making sure the texture has no holes.")]
    public bool AlwaysDrawBottomCube = true;

    [Tooltip("The gradient used to color the terrain. (The mesh has vertex color set and uses a simple shader which uses them)")]
    public Gradient HeighGradient;
    [Tooltip("Offset used for the coloring. (between -1 and 1)")]
    public float ColorOffset = 0;
    [Tooltip("The scaling factor used for the coloring. Normally a point with maximal height has the color at point 1 in the gradient, this changes the behaviour.")]
    public float ColorScaling = 1;
    [Tooltip("Which Algorithm should be used in order to generate the mesh.")]
    public VisualitationType VisualisationMethod;

    [Tooltip("The name of the mesh, if it should be saved to a file.")]
    public string FileName = "TestMesh";

    [Tooltip("The generated Mesh. Can be double clicked to inspect. (And accessed from other scripts)")]
    public Mesh GeneratedMesh;
    [Tooltip("The used profile for the settings above. Different profiles allow to easy save and switch profiles for different kinds of pcitures.")]
    public MeshCreationSettings UsedProfile;



    //Linking only uncomment the HideInInspector if the link is somehow lost
    [HideInInspector]
    public MeshFilter TargetMesh;

    //No need to show in the editor, can be accessed from other scripts. 
    [HideInInspector]
    public DataGrid Data2D;
    [HideInInspector]
    public Data3D Data3D;

    //Path to the generatedMeshes folder change if you move this plugin around
    const string path = "Assets/Examples/TerrainGeneration/GeneratedMeshes/";


    /// <summary>
    /// Applying simple QuantumBlur (the Unity implementation) to the TextureToBlur
    /// The new Image becomes the InputTexture (Which can also be set directly)
    /// </summary>
    public void ApplyBlur(bool clearData = true) {

        if (TextureToBlur == null) {
            Debug.LogError("No texture to blur specified!");
            return;
        }

        if (!TextureToBlur.isReadable) {
            Debug.LogError("TextureToBlur is not readable. Select the image and in the Inspector window go to 'Advanced' and set 'Read / Write Enabled' to true (tick the box)");
        }



        InputTexture = QuantumBlurUsage.CalculateUnityBlur(TextureToBlur, BlurRotation);
        if (clearData) {
            //Clearing data
            Data3D = null;
            Data2D = null;
        }
    }

    /// <summary>
    /// Applying your own effect (implemented in QuantumBlurUsages "CalculateMyOwnEffect" function) to your image
    /// </summary>
    public void ApplyYourOwnEffect(bool clearData = true) {

        if (TextureToBlur == null) {
            Debug.LogError("No texture to blur specified!");
            return;
        }

        if (!TextureToBlur.isReadable) {
            Debug.LogError("TextureToBlur is not readable. Select the image and in the Inspector window go to 'Advanced' and set 'Read / Write Enabled' to true (tick the box)");
        }

        InputTexture = QuantumBlurUsage.CalculateMyOwnEffect(TextureToBlur, BlurRotation);
        if (clearData) {
            //Clearing data
            Data3D = null;
            Data2D = null;
        }
    }

    /// <summary>
    /// Transforming the image into 2D Data. (An internal 2D float array).
    /// </summary>
    public void Generate2DData() {

        if (InputTexture == null) {
            Debug.LogWarning("No input texture specified, trying to blur texture");
            ApplyBlur(false);
            if (InputTexture == null) {
                return;
            }
        }

        if (!InputTexture.isReadable) {
            Debug.LogError("InputTexture is not readable. Select the image and in the Inspector window go to 'Advanced' and set 'Read / Write Enabled' to true (tick the box)");
        }

        int width = InputTexture.width;
        int height = InputTexture.height;

        Data2D = new DataGrid(width, height);

        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                Color color = InputTexture.GetPixel(i, j);
                float luma = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
                if (Invert) {
                    luma = 1 - luma;
                }
                Data2D[i, j].Value = luma;
            }
        }
    }

    /// <summary>
    /// Transforming the 2D data in a 3D array according to height.
    /// This is needed for several methods using a cube grid to generate the landscape.
    /// </summary>
    public void Generate3DDataFrom2DData() {
        if (Data2D == null) {
            Generate2DData();
        }
        int width = InputTexture.width;
        int height = InputTexture.height;
        int vertical = MaxHeight + 1;


        float maxValue = 1.0f / MaxHeight;
        float currMax = 0;

        int startZ = 0;

        if (AlwaysDrawBottomCube) {
            vertical = vertical + 1;
            Data3D = new Data3D(width, height, vertical);
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    for (int z = 0; z < 1; z++) {
                        Data3D[x, y, z].Value = 1;
                    }
                }
            }
            startZ = 1;
        } else {
            Data3D = new Data3D(width, height, vertical);
        }

        for (int z = startZ; z < vertical - 1; z++) {
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    if (AlwaysDrawBottomCube && z == 1) {
                        Data3D[x, y, z].Value = 1;
                    } else {
                        Data3D[x, y, z].Value = Mathf.Clamp((Data2D[x, y].Value - currMax) * MaxHeight, 0, 1);
                    }
                }
            }
            currMax += maxValue;
        }
    }

    /// <summary>
    /// Generating a mesh (terrain) representation of the chosen image, according to the selected visualisation method.
    /// </summary>
    public void GenerateMesh(bool doColoring = true) {

        if (VisualisationMethod == VisualitationType.Noise2D) {
            if (Data2D == null) {
                Generate2DData();
            }
        } else {
            if (Data3D == null) {
                Generate3DDataFrom2DData();
            }
        }

        switch (VisualisationMethod) {
            case VisualitationType.Noise2D:
                GeneratedMesh = Generate2DNoise(Data2D, Threshold);
                break;
            case VisualitationType.Noise3D:
                GeneratedMesh = Generate3DNoise(Data3D, Threshold, AlwaysDrawBottomCube);
                break;
            case VisualitationType.Terrain:
                GeneratedMesh = GenerateTerrain(Data2D, MaxHeight);
                break;
            case VisualitationType.MarshingCubes:
                GeneratedMesh = GenerateMarshingCubes(Data3D, Threshold, MaxHeight, AlwaysDrawBottomCube);
                break;
            case VisualitationType.MarshingCubesAdvanced:
                GeneratedMesh = GenerateMarshingCubesAdvanced(Data3D, Threshold, MaxHeight, AlwaysDrawBottomCube);
                break;
            default:
                break;
        }

        if (doColoring) {
            ColorMesh();
        }

        if (TargetMesh!=null) {
            TargetMesh.mesh = GeneratedMesh;

        } else {
            Debug.LogError("TargetMesh is null, need to be linked again");
        }
    }

    /// <summary>
    /// Using a method similar to visualising 2D (perlin) noise.
    /// Blocks are either there or not according to the brightness of the picture is over the threshold.
    /// </summary>
    /// <param name="data2D">the 2D data of the image</param>
    /// <param name="threshold">Value between 0 and 1. The "noise" threshold the lower it is the more blocks are visualised</param>
    /// <returns>A mesh representing the image.</returns>
    public static Mesh Generate2DNoise(DataGrid data2D, float threshold) {

        Vector3 StartPos = new Vector3(-0.5f * data2D.X + 0.5f, 0.5f, -0.5f * data2D.Y - 0.5f);
        Vector3 spawnPosition;


        List<Vector3> positions = new List<Vector3>();
        List<bool[]> ignoreFace = new List<bool[]>();
        for (int i = 0; i < data2D.X; i++) {
            for (int j = 0; j < data2D.Y; j++) {
                if (data2D[i, j].Value > threshold) {
                    spawnPosition = StartPos + new Vector3(i, 0, j);
                    positions.Add(spawnPosition);
                    bool[] ignore = new bool[6] {
                        j>0 && data2D[i,j-1].Value>threshold,
                        j<data2D.Y-1 && data2D[i,j+1].Value>threshold,
                        false,
                        i<data2D.X-1 && data2D[i+1,j].Value>threshold,
                        true,
                        i>0 && data2D[i-1,j].Value>threshold,

                    };
                    ignoreFace.Add(ignore);
                }
            }
        }
        return MeshGenerator.GetCubes(positions, ignoreFace, Vector3.one);
    }

    /// <summary>
    /// Generating a mesh interpreting the 3D data as noise and generating cubes in a 3D grid if the noise is over the threshold
    /// </summary>
    /// <param name="data3D">The 3D data representation of the image (or of anything else if you want).</param>
    /// <param name="threshold">Value between 0 and 1. A good value here is normally 0.5 Different values can be appropriate for different (non image) data.</param>
    /// <param name="alwaysDrawBottomCube">With this parameter set the lowest place of cubes is always drawn. Making sure there are no "holes"</param>
    /// <returns></returns>
    public static Mesh Generate3DNoise(Data3D data3D, float threshold, bool alwaysDrawBottomCube = true) {
        Vector3 StartPos = new Vector3(-0.5f * data3D.X + 0.5f, 0.5f, -0.5f * data3D.Y - 0.5f);
        if (alwaysDrawBottomCube) {
            StartPos = new Vector3(-0.5f * data3D.X + 0.5f, -0.5f, -0.5f * data3D.Y - 0.5f);
        }
        Vector3 spawnPosition;



        List<Vector3> positions = new List<Vector3>();
        List<bool[]> ignoreFace = new List<bool[]>();
        for (int i = 0; i < data3D.X; i++) {
            for (int j = 0; j < data3D.Y; j++) {
                for (int k = 0; k < data3D.Z; k++) {


                    if (data3D[i, j, k].Value > threshold) {
                        spawnPosition = StartPos + new Vector3(i, k, j);
                        positions.Add(spawnPosition);
                        bool[] ignore = new bool[6] {
                            j>0 && data3D[i,j-1,k].Value>threshold,
                            j<data3D.Y-1 && data3D[i,j+1,k].Value>threshold,
                            k<data3D.Z-1 && data3D[i,j,k+1].Value>threshold,
                            i<data3D.X-1 && data3D[i+1,j,k].Value>threshold,
                            (k==0) || data3D[i,j,k-1].Value>threshold,
                            i>0 && data3D[i-1,j,k].Value>threshold,

                        };
                        ignoreFace.Add(ignore);
                    }
                }
            }
        }
        return MeshGenerator.GetCubes(positions, ignoreFace, Vector3.one);
    }

    /// <summary>
    /// Generates a Terrain (deformed plane) according to the 2D data
    /// </summary>
    /// <param name="data2D">The height data representation of the image</param>
    /// <param name="maxHeight">The maximum height the terrain should have </param>
    /// <returns>Returns a mesh representing the generated terrain.</returns>
    public static Mesh GenerateTerrain(DataGrid data2D, int maxHeight) {
        return MeshGenerator.ConstructGrid(data2D, maxHeight);
    }

    /// <summary>
    /// Generate a mesh using a (simple) Marching Cubes algorithm. 
    /// </summary>
    /// <param name="data3D">The 3D data representation of the image (or of anything else if you want).</param>
    /// <param name="threshold">Value between 0 and 1. A good value here is normally 0.5 Different values will create (in average) more steap or less cheap angles.</param>
    /// <param name="maxHeight">The maximum height the mesh will have</param>
    /// <param name="alwaysDrawBottomCube">With this parameter set the lowest place of cubes is always drawn. Making sure there are no "holes"</param>
    /// <returns>Returns a mesh being made with marching cubes representing the image.</returns>
    public static Mesh GenerateMarshingCubes(Data3D data3D, float threshold, int maxHeight, bool alwaysDrawBottomCube = true) {
        float starty = 0.5f * maxHeight + 1;
        if (alwaysDrawBottomCube) {
            starty = starty - 0.5f;
        }
        return MeshGenerator.ConstructMarchingCubesYZSwitched(data3D, new Vector3(0, starty, 0), threshold);
    }

    /// <summary>
    /// Generate a mesh using an advanced Marching Cubes algorithm, allowing for different angels.
    /// </summary>
    /// <param name="data3D">The 3D data representation of the image (or of anything else if you want).</param>
    /// <param name="threshold">Value between 0 and 1. A good value here is normally 0.5 Different values will create (in average) more steap or less cheap angles.</param>
    /// <param name="maxHeight">The maximum height the mesh will have</param>
    /// <param name="alwaysDrawBottomCube">With this parameter set the lowest place of cubes is always drawn. Making sure there are no "holes"</param>
    /// <returns>Returns a mesh being made with the advanced marching cubes representing the image.</returns>

    public static Mesh GenerateMarshingCubesAdvanced(Data3D data3D, float threshold, int maxHeight, bool alwaysDrawBottomCube = true) {
        float starty = 0.5f * maxHeight + 1;
        if (alwaysDrawBottomCube) {
            starty = starty - 0.5f;
        }
        return MeshGenerator.ConstructMarchingCubesYZSwitched(data3D, new Vector3(0, starty, 0), Color.white, threshold);
    }

    /// <summary>
    /// Generates a save path for the mesh to be saved to. Is used since creating assets is only allowed in editor scripts.
    /// </summary>
    /// <returns></returns>
    public string GenerateSavePath() {
        string filepath = Path.Combine(path, FileName + ".Asset");
        return filepath;
    }

    /// <summary>
    /// Colors the mesh. (Setting the color parameters) according to the chosen parameters (Heighgradient, Colorscaling and ColorOffset).
    /// </summary>
    public void ColorMesh() {

        if (GeneratedMesh==null) {
            Debug.LogWarning("There is no mesh generated. Generating mesh.");
            GenerateMesh(false);
        }

        Vector3[] vertices = GeneratedMesh.vertices;

        int vertexCount = vertices.Length;

        Color[] colors = new Color[vertexCount];
        for (int i = 0; i < vertexCount; i++) {
            colors[i] = HeighGradient.Evaluate((vertices[i].y / MaxHeight) * ColorScaling + ColorOffset);
        }

        GeneratedMesh.colors = colors;
    }

    /// <summary>
    /// Saving the settings to the selected settings file (profile)
    /// </summary>
    public void SaveSettings() {
        UsedProfile.MaxHeight = MaxHeight;
        UsedProfile.Invert = Invert;
        UsedProfile.Threshold = Threshold;
        UsedProfile.HeighGradient = HeighGradient;
        UsedProfile.AlwaysDrawBottomCube = AlwaysDrawBottomCube;
        UsedProfile.ColorTranslation = ColorOffset;
        UsedProfile.ColorScaling = ColorScaling;
        UsedProfile.VisualisationMethod = VisualisationMethod;
        UsedProfile.BlurRotation = BlurRotation;
    }

    /// <summary>
    /// Loading the settings from a settings file (profile)
    /// </summary>
    public void LoadSettings() {
        MaxHeight = UsedProfile.MaxHeight;
        Invert = UsedProfile.Invert;
        Threshold = UsedProfile.Threshold;
        HeighGradient = UsedProfile.HeighGradient;
        AlwaysDrawBottomCube = UsedProfile.AlwaysDrawBottomCube;
        ColorOffset = UsedProfile.ColorTranslation;
        ColorScaling = UsedProfile.ColorScaling;
        VisualisationMethod = UsedProfile.VisualisationMethod;
        BlurRotation = UsedProfile.BlurRotation;
    }


    /// <summary>
    /// Types an image can be visualised as terrain.
    /// </summary>
    public enum VisualitationType {
        Noise2D,
        Noise3D,
        Terrain,
        MarshingCubes,
        MarshingCubesAdvanced
    }

}
