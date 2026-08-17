using System.Collections.Generic;
using UnityEngine;

namespace Formify.Domain
{
    /// <summary>ROOM-01: builds a closed room (N walls + floor + ceiling) from an ordered XZ footprint.</summary>
    public static class RoomBuilder
    {
        public static RoomDefinition Build(IReadOnlyList<Vector2> footprint, float height, float thickness)
        {
            if (footprint == null || footprint.Count < 3 || height <= 0f || thickness <= 0f) return null;

            // Walls use right = edge direction, up = +Y, so Normal = Cross(right, up) = (-dz, 0, dx).
            // That points into the interior only for a counter-clockwise footprint (positive shoelace
            // area in XZ); a clockwise footprint is reversed up front so the invariant always holds.
            var p = new List<Vector2>(footprint);
            if (SignedArea(p) < 0f) p.Reverse();

            var room = new RoomDefinition();

            for (int i = 0; i < p.Count; i++)
            {
                Vector2 a = p[i], b = p[(i + 1) % p.Count];
                var edge = new Vector3(b.x - a.x, 0f, b.y - a.y);
                room.surfaces.Add(new SurfaceDefinition
                {
                    id = room.surfaces.Count,
                    name = "Wall " + (i + 1),
                    kind = SurfaceKind.Wall,
                    origin = new Vector3(a.x, 0f, a.y),
                    right = edge.normalized,
                    up = Vector3.up,
                    width = edge.magnitude,
                    height = height,
                    thickness = thickness
                });
            }

            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var v in p)
            {
                minX = Mathf.Min(minX, v.x);
                maxX = Mathf.Max(maxX, v.x);
                minZ = Mathf.Min(minZ, v.y);
                maxZ = Mathf.Max(maxZ, v.y);
            }
            float sizeX = maxX - minX, sizeZ = maxZ - minZ;

            // Floor basis: Cross(+X, +Z) = -Y (outward, wrong), so local up is BACK — Cross(+X, -Z) = +Y,
            // which points up into the room. Origin moves to the maxZ corner so local +up still sweeps
            // the bounding box: LocalToWorld(0,0) = (minX,0,maxZ), LocalToWorld(w,h) = (maxX,0,minZ).
            room.surfaces.Add(new SurfaceDefinition
            {
                id = room.surfaces.Count,
                name = "Floor",
                kind = SurfaceKind.Floor,
                origin = new Vector3(minX, 0f, maxZ),
                right = Vector3.right,
                up = Vector3.back,
                width = sizeX,
                height = sizeZ,
                thickness = thickness
            });

            // Ceiling basis: Cross(+X, +Z) = -Y, i.e. down into the room — the pair that is already correct
            // here, swept from the minZ corner: LocalToWorld(w,h) = (maxX, height, maxZ).
            room.surfaces.Add(new SurfaceDefinition
            {
                id = room.surfaces.Count,
                name = "Ceiling",
                kind = SurfaceKind.Ceiling,
                origin = new Vector3(minX, height, minZ),
                right = Vector3.right,
                up = Vector3.forward,
                width = sizeX,
                height = sizeZ,
                thickness = thickness
            });

            return room;
        }

        /// <summary>Shoelace area over (x, z). Positive = counter-clockwise = walls already face inward.</summary>
        static float SignedArea(List<Vector2> p)
        {
            float a = 0f;
            for (int i = 0; i < p.Count; i++)
            {
                Vector2 c = p[i], n = p[(i + 1) % p.Count];
                a += c.x * n.y - n.x * c.y;
            }
            return a * 0.5f;
        }
    }
}
