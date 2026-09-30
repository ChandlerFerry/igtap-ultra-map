using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using UnityEngine;

static class WorldExport
{
    const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    static readonly HashSet<string> Skipped = new HashSet<string> { "currentPlayerPath", "currentPlayerSprites", "currentPlayerScales" };

    public static string Write(string dir)
    {
        Movement player = UnityEngine.Object.FindAnyObjectByType<Movement>();
        if (player == null) throw new InvalidOperationException("no player");
        Directory.CreateDirectory(dir);
        WriteWorld(Path.Combine(dir, "map.world.json"));
        string trace = Path.Combine(dir, "map.json");
        using (var writer = new StreamWriter(trace))
        {
            var json = new ExportJson(writer);
            JsonTextWriter w = json.W;
            w.WriteStartObject();
            w.WritePropertyName("course"); w.WriteValue(0);
            w.WritePropertyName("fixedDeltaTime"); json.Float(Time.fixedDeltaTime);
            w.WritePropertyName("start"); WriteStart(json, player);
            w.WriteEndObject();
        }
        return trace;
    }

    static void WriteStart(ExportJson json, Movement player)
    {
        JsonTextWriter w = json.W;
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        w.WriteStartObject();
        w.WritePropertyName("tick"); w.WriteValue(0);
        w.WritePropertyName("random"); w.WriteStartArray();
        object random = UnityEngine.Random.state;
        foreach (FieldInfo f in typeof(UnityEngine.Random.State).GetFields(InstanceFields)) w.WriteValue((int)f.GetValue(random));
        w.WriteEndArray();
        w.WritePropertyName("movement"); WriteFields(json, player, f => f.Name == "GamePaused" ? (object)false : null);
        w.WritePropertyName("engine"); w.WriteStartArray(); w.WriteEndArray();
        w.WritePropertyName("courses"); w.WriteStartArray();
        foreach (courseScript course in UnityEngine.Object.FindObjectsByType<courseScript>(FindObjectsSortMode.None))
        {
            w.WriteStartObject();
            w.WritePropertyName("id"); w.WriteValue(course.GetInstanceID());
            w.WritePropertyName("fields"); WriteFields(json, course, _ => null);
            w.WriteEndObject();
        }
        w.WriteEndArray();
        w.WritePropertyName("position"); json.Value(player.transform.position, 0);
        w.WritePropertyName("bodyPosition"); json.Value(body.position, 0);
        w.WritePropertyName("velocity"); json.Value(body.linearVelocity, 0);
        w.WritePropertyName("rotation"); json.Float(body.rotation);
        w.WritePropertyName("angularVelocity"); json.Float(body.angularVelocity);
        w.WritePropertyName("colliders"); w.WriteStartArray();
        foreach (BoxCollider2D c in player.GetComponents<BoxCollider2D>())
        {
            w.WriteStartArray();
            json.Float(c.offset.x); json.Float(c.offset.y); json.Float(c.size.x); json.Float(c.size.y);
            w.WriteValue((int)c.excludeLayers); w.WriteValue(c.enabled);
            w.WriteEndArray();
        }
        w.WriteEndArray();
        w.WritePropertyName("pending"); w.WriteStartArray(); w.WriteEndArray();
        if (VmanScript.isCurrentlyVman)
        {
            w.WritePropertyName("vman"); w.WriteStartObject();
            w.WritePropertyName("timeLeft"); w.WriteValue(VmanScript.vmanTimeLeft);
            w.WritePropertyName("score"); w.WriteValue(VmanScript.vmanScore);
            w.WritePropertyName("blockEnding"); w.WriteValue(VmanScript.Instance != null && VmanScript.Instance.BlockEnding);
            w.WriteEndObject();
        }
        w.WritePropertyName("transformRotation"); json.Value(player.transform.rotation, 0);
        w.WritePropertyName("localScale"); json.Value(player.transform.localScale, 0);
        w.WriteEndObject();
    }

    static void WriteFields(ExportJson json, MonoBehaviour target, Func<FieldInfo, object> replace)
    {
        JsonTextWriter w = json.W;
        w.WriteStartArray();
        for (Type t = target.GetType(); t != null && t != typeof(MonoBehaviour) && t != typeof(object); t = t.BaseType)
            foreach (FieldInfo f in t.GetFields(InstanceFields))
            {
                if (typeof(Delegate).IsAssignableFrom(f.FieldType) || Skipped.Contains(f.Name)) continue;
                w.WriteStartArray();
                w.WriteValue(f.DeclaringType.FullName);
                w.WriteValue(f.Name);
                json.Value(replace(f) ?? f.GetValue(target), 0);
                w.WriteEndArray();
            }
        w.WriteEndArray();
    }

    static void WriteWorld(string path)
    {
        var gameObjects = new List<GameObject>();
        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            if (go != null && go.scene.IsValid() && (go.hideFlags & HideFlags.HideAndDontSave) == 0) gameObjects.Add(go);
        var materials = new Dictionary<int, PhysicsMaterial2D>();
        Dictionary<CompositeCollider2D, PhysicsShapeGroup2D> sourceShapes = ColouredBlockSourceShapes();
        using (var writer = new StreamWriter(path))
        {
            var json = new ExportJson(writer);
            JsonTextWriter w = json.W;
            w.WriteStartObject();
            w.WritePropertyName("schema"); w.WriteValue(1);
            w.WritePropertyName("unityVersion"); w.WriteValue(Application.unityVersion);
            w.WritePropertyName("createdUtc"); w.WriteValue(DateTime.UtcNow.ToString("o"));
            w.WritePropertyName("activeScene"); w.WriteValue(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            w.WritePropertyName("time");
            w.WriteStartObject();
            w.WritePropertyName("fixedDeltaTime"); json.Float(Time.fixedDeltaTime);
            w.WritePropertyName("timeScale"); json.Float(Time.timeScale);
            w.WritePropertyName("maximumDeltaTime"); json.Float(Time.maximumDeltaTime);
            w.WriteEndObject();
            w.WritePropertyName("physics2d"); WriteStaticProperties(json, typeof(Physics2D));
            w.WritePropertyName("layerCollisionMasks");
            w.WriteStartArray();
            for (int i = 0; i < 32; i++) w.WriteValue(Physics2D.GetLayerCollisionMask(i));
            w.WriteEndArray();
            w.WritePropertyName("layerNames");
            w.WriteStartArray();
            for (int i = 0; i < 32; i++) w.WriteValue(LayerMask.LayerToName(i));
            w.WriteEndArray();
            w.WritePropertyName("objects");
            w.WriteStartArray();
            foreach (GameObject go in gameObjects) WriteGameObject(json, go, materials, sourceShapes);
            w.WriteEndArray();
            w.WritePropertyName("materials");
            w.WriteStartArray();
            foreach (PhysicsMaterial2D m in materials.Values)
            {
                w.WriteStartObject();
                w.WritePropertyName("id"); w.WriteValue(m.GetInstanceID());
                w.WritePropertyName("name"); w.WriteValue(m.name);
                w.WritePropertyName("props"); WriteProperties(json, m);
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WritePropertyName("statics"); WriteGameStatics(json);
            w.WriteEndObject();
        }
    }

    static Dictionary<CompositeCollider2D, PhysicsShapeGroup2D> ColouredBlockSourceShapes()
    {
        var result = new Dictionary<CompositeCollider2D, PhysicsShapeGroup2D>();
        colouredBlockSwapper swapper = UnityEngine.Object.FindAnyObjectByType<colouredBlockSwapper>(FindObjectsInactive.Include);
        if (swapper == null) return result;
        var composites = new List<(CompositeCollider2D composite, Collider2D[] sources, int paths, int points, int shapes)>();
        foreach (string colour in new[] { "orange", "blue" })
            foreach (GameObject o in (GameObject[])typeof(colouredBlockSwapper).GetField(colour, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(swapper) ?? new GameObject[0])
            {
                CompositeCollider2D composite = o == null ? null : o.GetComponent<CompositeCollider2D>();
                if (composite == null) continue;
                Collider2D[] sources = o.GetComponents<Collider2D>()
                    .Where(c => c != composite && c.compositeOperation != Collider2D.CompositeOperation.None).ToArray();
                composites.Add((composite, sources, composite.pathCount, composite.pointCount, composite.shapeCount));
            }
        var switchedOn = new List<Collider2D>();
        try
        {
            foreach (var c in composites)
                foreach (Collider2D source in c.sources)
                    if (!source.enabled) { source.enabled = true; switchedOn.Add(source); }
            foreach (var c in composites)
            {
                foreach (Collider2D source in c.sources) source.GetType().GetMethod("ProcessTilemapChanges", Type.EmptyTypes)?.Invoke(source, null);
                c.composite.GenerateGeometry();
                var group = new PhysicsShapeGroup2D();
                c.composite.GetShapes(group);
                result[c.composite] = group;
            }
        }
        finally
        {
            foreach (Collider2D source in switchedOn) source.enabled = false;
            foreach (var c in composites)
            {
                foreach (Collider2D source in c.sources) source.GetType().GetMethod("ProcessTilemapChanges", Type.EmptyTypes)?.Invoke(source, null);
                c.composite.GenerateGeometry();
            }
        }
        foreach (var c in composites)
            if (c.composite.pathCount != c.paths || c.composite.pointCount != c.points || c.composite.shapeCount != c.shapes)
                throw new InvalidOperationException("World export: " + c.composite.name + "'s composite didn't come back as it was ("
                    + c.paths + "/" + c.points + "/" + c.shapes + " paths/points/shapes, now " + c.composite.pathCount + "/"
                    + c.composite.pointCount + "/" + c.composite.shapeCount + ").");
        return result;
    }

    static void WriteGameObject(ExportJson json, GameObject go, Dictionary<int, PhysicsMaterial2D> materials,
        Dictionary<CompositeCollider2D, PhysicsShapeGroup2D> sourceShapes)
    {
        JsonTextWriter w = json.W;
        Transform t = go.transform;
        w.WriteStartObject();
        w.WritePropertyName("id"); w.WriteValue(go.GetInstanceID());
        w.WritePropertyName("name"); w.WriteValue(go.name);
        w.WritePropertyName("scene"); w.WriteValue(go.scene.name);
        w.WritePropertyName("parent"); w.WriteValue(t.parent == null ? 0 : t.parent.gameObject.GetInstanceID());
        w.WritePropertyName("siblingIndex"); w.WriteValue(t.GetSiblingIndex());
        w.WritePropertyName("activeSelf"); w.WriteValue(go.activeSelf);
        w.WritePropertyName("activeInHierarchy"); w.WriteValue(go.activeInHierarchy);
        w.WritePropertyName("layer"); w.WriteValue(go.layer);
        w.WritePropertyName("tag"); w.WriteValue(go.tag);
        w.WritePropertyName("transformId"); w.WriteValue(t.GetInstanceID());
        w.WritePropertyName("position"); json.Value(t.position, 0);
        w.WritePropertyName("rotation"); json.Value(t.rotation, 0);
        w.WritePropertyName("lossyScale"); json.Value(t.lossyScale, 0);
        w.WritePropertyName("localPosition"); json.Value(t.localPosition, 0);
        w.WritePropertyName("localRotation"); json.Value(t.localRotation, 0);
        w.WritePropertyName("localScale"); json.Value(t.localScale, 0);
        w.WritePropertyName("components");
        w.WriteStartArray();
        foreach (Component c in go.GetComponents<Component>())
        {
            if (c == null || c is Transform) continue;
            w.WriteStartObject();
            w.WritePropertyName("id"); w.WriteValue(c.GetInstanceID());
            w.WritePropertyName("type"); w.WriteValue(c.GetType().FullName);
            w.WritePropertyName("assembly"); w.WriteValue(c.GetType().Assembly.GetName().Name);
            if (c is Behaviour b) { w.WritePropertyName("enabled"); w.WriteValue(b.enabled); }
            if (c is MonoBehaviour mb && c.GetType().Assembly == typeof(Movement).Assembly)
            {
                w.WritePropertyName("fields");
                WriteAllFields(json, mb);
            }
            else if (c is Collider2D col)
            {
                if (col.sharedMaterial != null) materials[col.sharedMaterial.GetInstanceID()] = col.sharedMaterial;
                w.WritePropertyName("props"); WriteProperties(json, col);
                w.WritePropertyName("shapes"); WriteShapes(json, col);
                WriteColliderPaths(json, col);
                if (col is CompositeCollider2D composite && sourceShapes.TryGetValue(composite, out PhysicsShapeGroup2D group))
                {
                    w.WritePropertyName("sourceShapes");
                    WriteShapeGroup(json, group, group.shapeCount);
                }
            }
            else if (c is Rigidbody2D rb)
            {
                if (rb.sharedMaterial != null) materials[rb.sharedMaterial.GetInstanceID()] = rb.sharedMaterial;
                w.WritePropertyName("props"); WriteProperties(json, rb);
            }
            else if (c is Effector2D || c is Joint2D)
            {
                w.WritePropertyName("props"); WriteProperties(json, c);
            }
            w.WriteEndObject();
        }
        w.WriteEndArray();
        w.WriteEndObject();
    }

    static void WriteAllFields(ExportJson json, MonoBehaviour target)
    {
        JsonTextWriter w = json.W;
        w.WriteStartArray();
        for (Type t = target.GetType(); t != null && t != typeof(MonoBehaviour) && t != typeof(object); t = t.BaseType)
            foreach (FieldInfo f in t.GetFields(InstanceFields))
            {
                if (typeof(Delegate).IsAssignableFrom(f.FieldType)) continue;
                object value;
                try { value = f.GetValue(target); }
                catch (Exception) { continue; }
                w.WriteStartArray();
                w.WriteValue(t.FullName);
                w.WriteValue(f.Name);
                json.Value(value, 0);
                w.WriteEndArray();
            }
        w.WriteEndArray();
    }

    static readonly List<Vector2> ShapeVertices = new List<Vector2>();

    static void WriteShapes(ExportJson json, Collider2D collider)
    {
        var group = new PhysicsShapeGroup2D();
        int count;
        try { count = collider.isActiveAndEnabled ? collider.GetShapes(group) : OffShapes(collider, group); }
        catch (Exception) { json.W.WriteNull(); return; }
        WriteShapeGroup(json, group, count);
    }

    static int OffShapes(Collider2D collider, PhysicsShapeGroup2D group)
    {
        if (!(collider is BoxCollider2D || collider is CapsuleCollider2D || collider is CircleCollider2D || collider is PolygonCollider2D
            || collider is EdgeCollider2D) || collider.compositeOperation != Collider2D.CompositeOperation.None) return 0;
        Rigidbody2D body = collider.attachedRigidbody;
        Transform t = collider.transform;
        var root = new GameObject("UltraMapShapeProbe");
        try
        {
            GameObject probe = root;
            if (body != null)
            {
                root.transform.SetPositionAndRotation(body.transform.position, body.transform.rotation);
                root.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                probe = new GameObject("UltraMapShapeProbeCollider");
                probe.transform.SetParent(root.transform, false);
            }
            probe.transform.SetPositionAndRotation(t.position, t.rotation);
            probe.transform.localScale = t.lossyScale;
            switch (collider)
            {
                case BoxCollider2D box:
                    {
                        BoxCollider2D copy = probe.AddComponent<BoxCollider2D>();
                        copy.size = box.size;
                        copy.edgeRadius = box.edgeRadius;
                        copy.offset = box.offset;
                        return copy.GetShapes(group);
                    }
                case CapsuleCollider2D capsule:
                    {
                        CapsuleCollider2D copy = probe.AddComponent<CapsuleCollider2D>();
                        copy.size = capsule.size;
                        copy.direction = capsule.direction;
                        copy.offset = capsule.offset;
                        return copy.GetShapes(group);
                    }
                case CircleCollider2D circle:
                    {
                        CircleCollider2D copy = probe.AddComponent<CircleCollider2D>();
                        copy.radius = circle.radius;
                        copy.offset = circle.offset;
                        return copy.GetShapes(group);
                    }
                case PolygonCollider2D polygon:
                    {
                        PolygonCollider2D copy = probe.AddComponent<PolygonCollider2D>();
                        var points = new List<Vector2>();
                        copy.pathCount = polygon.pathCount;
                        for (int p = 0; p < polygon.pathCount; p++)
                        {
                            points.Clear();
                            polygon.GetPath(p, points);
                            copy.SetPath(p, points);
                        }
                        copy.useDelaunayMesh = polygon.useDelaunayMesh;
                        copy.offset = polygon.offset;
                        return copy.GetShapes(group);
                    }
                case EdgeCollider2D edge:
                    {
                        EdgeCollider2D copy = probe.AddComponent<EdgeCollider2D>();
                        copy.points = edge.points;
                        copy.edgeRadius = edge.edgeRadius;
                        copy.useAdjacentStartPoint = edge.useAdjacentStartPoint;
                        copy.useAdjacentEndPoint = edge.useAdjacentEndPoint;
                        copy.adjacentStartPoint = edge.adjacentStartPoint;
                        copy.adjacentEndPoint = edge.adjacentEndPoint;
                        copy.offset = edge.offset;
                        return copy.GetShapes(group);
                    }
                default: return 0;
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void WriteShapeGroup(ExportJson json, PhysicsShapeGroup2D group, int count)
    {
        JsonTextWriter w = json.W;
        w.WriteStartObject();
        w.WritePropertyName("localToWorld");
        Matrix4x4 m = group.localToWorldMatrix;
        w.WriteStartArray();
        for (int i = 0; i < 16; i++) json.Float(m[i]);
        w.WriteEndArray();
        w.WritePropertyName("list");
        w.WriteStartArray();
        for (int i = 0; i < count; i++)
        {
            PhysicsShape2D shape = group.GetShape(i);
            ShapeVertices.Clear();
            group.GetShapeVertices(i, ShapeVertices);
            w.WriteStartObject();
            w.WritePropertyName("type"); w.WriteValue(shape.shapeType.ToString());
            w.WritePropertyName("radius"); json.Float(shape.radius);
            w.WritePropertyName("vertices");
            w.WriteStartArray();
            foreach (Vector2 v in ShapeVertices) { json.Float(v.x); json.Float(v.y); }
            w.WriteEndArray();
            w.WritePropertyName("useAdjacentStart"); w.WriteValue(shape.useAdjacentStart);
            w.WritePropertyName("useAdjacentEnd"); w.WriteValue(shape.useAdjacentEnd);
            w.WritePropertyName("adjacentStart"); json.Value(shape.adjacentStart, 0);
            w.WritePropertyName("adjacentEnd"); json.Value(shape.adjacentEnd, 0);
            w.WriteEndObject();
        }
        w.WriteEndArray();
        w.WriteEndObject();
    }

    static void WriteColliderPaths(ExportJson json, Collider2D collider)
    {
        JsonTextWriter w = json.W;
        var points = new List<Vector2>();
        if (collider is CompositeCollider2D composite)
        {
            w.WritePropertyName("paths");
            w.WriteStartArray();
            for (int p = 0; p < composite.pathCount; p++)
            {
                points.Clear();
                composite.GetPath(p, points);
                w.WriteStartArray();
                foreach (Vector2 v in points) { json.Float(v.x); json.Float(v.y); }
                w.WriteEndArray();
            }
            w.WriteEndArray();
        }
        else if (collider is PolygonCollider2D polygon)
        {
            w.WritePropertyName("paths");
            w.WriteStartArray();
            for (int p = 0; p < polygon.pathCount; p++)
            {
                points.Clear();
                polygon.GetPath(p, points);
                w.WriteStartArray();
                foreach (Vector2 v in points) { json.Float(v.x); json.Float(v.y); }
                w.WriteEndArray();
            }
            w.WriteEndArray();
        }
        else if (collider is EdgeCollider2D edge)
        {
            w.WritePropertyName("paths");
            w.WriteStartArray();
            w.WriteStartArray();
            foreach (Vector2 v in edge.points) { json.Float(v.x); json.Float(v.y); }
            w.WriteEndArray();
            w.WriteEndArray();
        }
    }

    static bool SimpleType(Type t)
    {
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(Vector2) || t == typeof(Vector3) || t == typeof(Vector4)
            || t == typeof(Quaternion) || t == typeof(Color) || t == typeof(Rect) || t == typeof(Bounds) || t == typeof(LayerMask)
            || t == typeof(Vector2Int) || typeof(UnityEngine.Object).IsAssignableFrom(t);
    }

    static void WriteProperties(ExportJson json, object target)
    {
        JsonTextWriter w = json.W;
        w.WriteStartObject();
        foreach (PropertyInfo p in target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!p.CanRead || p.GetIndexParameters().Length > 0 || !SimpleType(p.PropertyType)) continue;
            if (p.IsDefined(typeof(ObsoleteAttribute), true)) continue;
            if (p.Name == "gameObject" || p.Name == "transform" || p.Name == "hideFlags") continue;
            object value;
            try { value = p.GetValue(target, null); }
            catch (Exception) { continue; }
            w.WritePropertyName(p.Name);
            json.Value(value, 0);
        }
        w.WriteEndObject();
    }

    static void WriteStaticProperties(ExportJson json, Type type)
    {
        JsonTextWriter w = json.W;
        w.WriteStartObject();
        foreach (PropertyInfo p in type.GetProperties(BindingFlags.Static | BindingFlags.Public))
        {
            if (!p.CanRead || p.GetIndexParameters().Length > 0 || !SimpleType(p.PropertyType)) continue;
            if (p.IsDefined(typeof(ObsoleteAttribute), true)) continue;
            object value;
            try { value = p.GetValue(null, null); }
            catch (Exception) { continue; }
            w.WritePropertyName(p.Name);
            json.Value(value, 0);
        }
        w.WriteEndObject();
    }

    static void WriteGameStatics(ExportJson json)
    {
        JsonTextWriter w = json.W;
        Type[] gameTypes;
        try { gameTypes = typeof(Movement).Assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { gameTypes = ex.Types.Where(t => t != null).ToArray(); }
        w.WriteStartArray();
        foreach (Type type in gameTypes)
        {
            if (type.IsGenericTypeDefinition) continue;
            FieldInfo[] fields;
            try { fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly); }
            catch (Exception) { continue; }
            foreach (FieldInfo f in fields)
            {
                if (f.IsLiteral || typeof(Delegate).IsAssignableFrom(f.FieldType)) continue;
                object value;
                try { value = f.GetValue(null); }
                catch (Exception) { continue; }
                w.WriteStartArray();
                w.WriteValue(type.FullName);
                w.WriteValue(f.Name);
                json.Value(value, 0);
                w.WriteEndArray();
            }
        }
        Type singleton = typeof(Movement).Assembly.GetType("Singleton`1");
        if (singleton != null)
            foreach (Type type in gameTypes)
            {
                if (type.IsAbstract || type.IsGenericTypeDefinition || type.BaseType == null || !type.BaseType.IsGenericType
                    || type.BaseType.GetGenericTypeDefinition() != singleton) continue;
                foreach (FieldInfo f in type.BaseType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    object value;
                    try { value = f.GetValue(null); }
                    catch (Exception) { continue; }
                    w.WriteStartArray();
                    w.WriteValue("Singleton`1[" + type.FullName + "]");
                    w.WriteValue(f.Name);
                    json.Value(value, 0);
                    w.WriteEndArray();
                }
            }
        w.WriteEndArray();
    }
}

sealed class ExportJson
{
    public readonly JsonTextWriter W;
    readonly HashSet<object> _visiting = new HashSet<object>(ReferenceComparer.Instance);

    public ExportJson(TextWriter writer) { W = new JsonTextWriter(writer) { Formatting = Formatting.None }; }

    public void Float(float f)
    {
        if (float.IsNaN(f) || float.IsInfinity(f)) W.WriteValue(f.ToString(CultureInfo.InvariantCulture));
        else W.WriteRawValue(f.ToString("G9", CultureInfo.InvariantCulture));
    }

    public void Value(object value, int depth)
    {
        if (value == null) { W.WriteNull(); return; }
        if (value is UnityEngine.Object unityObject)
        {
            if (unityObject == null) { W.WriteNull(); return; }
            W.WriteStartObject();
            W.WritePropertyName("$ref"); W.WriteValue(unityObject.GetInstanceID());
            W.WriteEndObject();
            return;
        }
        Type type = value.GetType();
        if (value is float f) { Float(f); return; }
        if (value is double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) W.WriteValue(d.ToString(CultureInfo.InvariantCulture));
            else W.WriteRawValue(d.ToString("G17", CultureInfo.InvariantCulture));
            return;
        }
        if (type.IsEnum) { W.WriteValue(Convert.ToInt64(value, CultureInfo.InvariantCulture)); return; }
        if (value is Delegate || value is Type || value is MemberInfo || value is IntPtr || value is UIntPtr || value is Coroutine
            || value is IEnumerator || value is System.Threading.WaitHandle)
        { W.WriteNull(); return; }
        if (value is string || type.IsPrimitive) { W.WriteValue(value); return; }
        if (value is LayerMask mask) { W.WriteValue(mask.value); return; }
        if (depth > 8 || (!type.IsValueType && !_visiting.Add(value))) { W.WriteNull(); return; }
        try
        {
            if (value is IDictionary dictionary)
            {
                W.WriteStartObject();
                W.WritePropertyName("$dict");
                W.WriteStartArray();
                foreach (DictionaryEntry entry in dictionary)
                {
                    W.WriteStartArray();
                    Value(entry.Key, depth + 1);
                    Value(entry.Value, depth + 1);
                    W.WriteEndArray();
                }
                W.WriteEndArray();
                W.WriteEndObject();
                return;
            }
            if (value is IEnumerable sequence)
            {
                W.WriteStartArray();
                foreach (object item in sequence) Value(item, depth + 1);
                W.WriteEndArray();
                return;
            }
            if (!type.IsValueType && type.Assembly != typeof(Movement).Assembly) { W.WriteNull(); return; }
            W.WriteStartObject();
            for (Type t = type; t != null && t != typeof(object) && t != typeof(ValueType); t = t.BaseType)
                foreach (FieldInfo field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (typeof(Delegate).IsAssignableFrom(field.FieldType)) continue;
                    W.WritePropertyName(t == type ? field.Name : t.Name + "::" + field.Name);
                    object fieldValue;
                    try { fieldValue = field.GetValue(value); }
                    catch (Exception) { W.WriteNull(); continue; }
                    Value(fieldValue, depth + 1);
                }
            W.WriteEndObject();
        }
        finally
        {
            if (!type.IsValueType) _visiting.Remove(value);
        }
    }

    sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Instance = new ReferenceComparer();
        public new bool Equals(object a, object b) { return ReferenceEquals(a, b); }
        public int GetHashCode(object o) { return RuntimeHelpers.GetHashCode(o); }
    }
}
