using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Minimap : MonoBehaviour
{
    public Tilemap Tilemap;
    public Grid Grid;
    public const float CellSize = 5;

    public Tile BaseTile;


    private void Awake()
    {
        Grid.cellSize = new Vector3(CellSize, CellSize, CellSize);
    }


    public void AddChunk(Vector2Int chunk, Texture2D texture)
    {
        Vector3Int pos = new Vector3Int(chunk.x, chunk.y, 0);

        Sprite s = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), (texture.width + texture.height) / 2 / CellSize);
        s.name = "Chunk texture for " + chunk.ToString();

        // Set the tile if it has not already been set
        if (Tilemap.GetTile<Tile>(pos) == null)
        {
            Tilemap.SetTile(pos, Instantiate(BaseTile));
        }

        // Update the tile sprite
        Tile t = Tilemap.GetTile<Tile>(pos);
        t.sprite = s;

        Tilemap.RefreshTile(pos);
    }




}
