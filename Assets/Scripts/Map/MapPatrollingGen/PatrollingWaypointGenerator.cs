using System.Collections.Generic;
using System.Linq;
using DelaunatorSharp;
using Maes.Map.MapGen;
using UnityEngine;

namespace Maes.Map.MapPatrollingGen
{
    public class PatrollingWaypointGenerator
    {
        public static IEnumerable<Vertex> GetPossibleWaypoints(SimulationMap<Tile> simulationMap)
        {
            // Get all wall tiles
            var wallTiles = GetWallsTiles(simulationMap);
            
            // The delaunator library requires a list of IPoint, so we convert the Vector2Int to IPoint
            var points = new List<IPoint>();

            // The centerCoordinatePoints will be used to generate the centerpoints of the delaunator triangles
            var centerCoordinatePoints = new List<Vertex>();

            foreach (var wallTile in wallTiles)
            {
                if (IsCornerTile(wallTile, wallTiles))
                {
                    points.Add(new Point(wallTile.x, wallTile.y));
                }
            }

            var delaunator = new Delaunator(points.ToArray());
            foreach (var triangle in delaunator.Triangles)
            {
                var centerpoint = delaunator.GetCentroid(triangle);
                centerCoordinatePoints.Add(new Vertex(0, new Vector2Int((int)centerpoint.X, (int)centerpoint.Y)));
            }

            //TODO: Connect neighboring centerpoints with edges, currently only the centerpoints are generated
            return centerCoordinatePoints;
        }
        private static IEnumerable<Vector2Int> GetWallsTiles(SimulationMap<Tile> simulationMap)
        {
            var wallTiles = new List<Vector2Int>();
            var width = simulationMap.WidthInTiles;
            var height = simulationMap.HeightInTiles;

            // The outer wall is two tiles thick, so we start at 1 and end at width-1 and height-1
            for (var x = 1; x <= width-1; x++)
            {
                for (var y = 1; y <= height-1; y++)
                {
                    var tile = simulationMap.GetTileByLocalCoordinate(x, y);
                    var firstTri = tile.GetTriangles()[0];
                    if (Tile.IsWall(firstTri.Type))
                    {
                        wallTiles.Add(new Vector2Int(x, y));
                    }
                }
            }

            return wallTiles;
        }

        private static bool IsCornerTile(Vector2Int tile, IEnumerable<Vector2Int> tiles)
        {
            // Horizontal grid check
            if (tiles.Any(t => t == tile + Vector2Int.right)
            && tiles.Any(t => t == tile + Vector2Int.left)
            && !tiles.Any(t => t == tile + Vector2Int.up)
            && !tiles.Any(t => t == tile + Vector2Int.down))
            {
                return false;
            }

            // Vertical grid check
            if (tiles.Any(t => t == tile + Vector2Int.up)
            && tiles.Any(t => t == tile + Vector2Int.down)
            && !tiles.Any(t => t == tile + Vector2Int.right)
            && !tiles.Any(t => t == tile + Vector2Int.left))
            {
                return false;
            }
           
            // Returns true otherwise due to the fact that it is a corner tile
            return true;
        }
    }
}
