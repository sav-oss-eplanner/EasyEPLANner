using System.Collections.Generic;

namespace TechObject
{
    /// <summary>
    /// Переносимые ссылки на привязанные агрегаты: base_tech_object
    /// и технологический номер вместо глобальных индексов.
    /// </summary>
    public static class AttachedObjectReferences
    {
        public const string LuaFieldName = "attached_objects_refs";

        public static string FromIndices(ITechObjectManager manager,
            string indicesValue)
        {
            if (string.IsNullOrWhiteSpace(indicesValue))
                return string.Empty;

            var refs = new List<string>();
            foreach (string part in indicesValue.Split(' '))
            {
                if (!int.TryParse(part, out int index))
                    continue;

                TechObject attachedObject = manager.GetTObject(index);
                if (attachedObject?.BaseTechObject == null)
                    continue;

                refs.Add(FormatReference(attachedObject));
            }

            return string.Join(" ", refs);
        }

        public static string FormatReference(TechObject techObject)
        {
            return $"{techObject.BaseTechObject.EplanName}:{techObject.TechNumber}";
        }

        public static List<int> ToIndices(ITechObjectManager manager,
            string refsValue, IEnumerable<TechObject> preferredObjects = null)
        {
            var indices = new List<int>();
            if (string.IsNullOrWhiteSpace(refsValue))
                return indices;

            foreach (string part in refsValue.Split(' '))
            {
                if (!TryParseReference(part, out string baseObjectName,
                        out int techNumber))
                    continue;

                int index = ResolveIndex(manager, baseObjectName, techNumber,
                    preferredObjects);
                if (index > 0)
                    indices.Add(index);
            }

            return indices;
        }

        /// <summary>
        /// Восстановить привязанные агрегаты объекта по переносимым ссылкам.
        /// </summary>
        /// <param name="preferredObjects">Импортированные объекты, которым
        /// отдаётся приоритет при разрешении ссылок</param>
        public static void ApplyTo(ITechObjectManager manager,
            TechObject techObject, IEnumerable<TechObject> preferredObjects = null)
        {
            if (string.IsNullOrWhiteSpace(techObject.AttachedObjectsRefs))
                return;

            List<int> indices = ToIndices(manager,
                techObject.AttachedObjectsRefs, preferredObjects);
            if (indices.Count == 0)
                return;

            techObject.AttachedObjects.SetNewValue(string.Join(" ", indices));
            techObject.AttachedObjectsRefs = string.Empty;
        }

        private static int ResolveIndex(ITechObjectManager manager,
            string baseObjectName, int techNumber,
            IEnumerable<TechObject> preferredObjects)
        {
            if (preferredObjects != null)
            {
                foreach (TechObject techObject in preferredObjects)
                {
                    if (!MatchesReference(techObject, baseObjectName, techNumber))
                        continue;

                    int index = manager.GetTechObjectN(techObject);
                    if (index > 0)
                        return index;
                }
            }

            return manager.GetTechObjectN(baseObjectName, techNumber);
        }

        private static bool MatchesReference(TechObject techObject,
            string baseObjectName, int techNumber)
        {
            return techObject?.BaseTechObject != null &&
                techObject.BaseTechObject.EplanName.Equals(baseObjectName) &&
                techObject.TechNumber == techNumber;
        }

        private static bool TryParseReference(string part,
            out string baseObjectName, out int techNumber)
        {
            baseObjectName = string.Empty;
            techNumber = 0;

            if (string.IsNullOrWhiteSpace(part))
                return false;

            string[] segments = part.Split(':');
            switch (segments.Length)
            {
                case 2 when int.TryParse(segments[1], out techNumber):
                    baseObjectName = segments[0];
                    break;
                case 3 when int.TryParse(segments[2], out techNumber):
                    // Старый формат base:tech_type:n — tech_type игнорируется.
                    baseObjectName = segments[0];
                    break;
                default:
                    return false;
            }

            return !string.IsNullOrEmpty(baseObjectName);
        }
    }
}
