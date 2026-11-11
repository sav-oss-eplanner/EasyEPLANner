using System.Collections.Generic;
using System.Linq;
using TechObject.CheckStrategy;

namespace TechObject
{
    /// <summary>
    /// Класс проверки на корректность объектов в дереве объектов.
    /// </summary>
    public class TechObjectChecker
    {
        public TechObjectChecker(ITechObjectManager techObjectManager)
        {
            this.techObjectManager = techObjectManager;
        }

        /// <summary>
        /// Проверка технологического объекта
        /// на правильность ввода и др.
        /// </summary>
        /// <returns>Строка с ошибками</returns>
        public string Check()
        {
            var errors = string.Empty;

            errors += ObjectsFieldEquality(new TypeFieldEqualStrategy());
            errors += ObjectsFieldEquality(new MonitorFieldEqualStrategy());
            errors += ObjectsFieldEquality(new EplanNameFieldEqualStrategy());
            errors += SingleInstanceViolation();

            foreach (var obj in techObjectManager.GenericTechObjects)
            {
                errors += obj.Check();
            }

            foreach (var obj in techObjectManager.TechObjects)
            {
                errors += obj.Check();
            }

            return errors;

        }

        /// <summary>
        /// Проверить поля объектов на совпадение согласно стратегии
        /// </summary>
        /// <param name="strategy">Стратегия проверки</param>
        /// <returns></returns>
        private string ObjectsFieldEquality(IFieldEqualityStrategy strategy)
        {
            var errorsList = new List<string>();
            foreach (var obj in techObjectManager.TechObjects)
            {
                int[] matches = strategy.FindEqual(obj, techObjectManager);

                if (matches.Count() > 1)
                {
                    errorsList.Add($"У объектов {string.Join(",", matches)} " +
                        $"совпадает поле \"{strategy.FieldName}\"\n");
                }
            }

            errorsList = errorsList.Distinct().ToList();
            return string.Join("", errorsList);
        }

        private string SingleInstanceViolation()
        {
            var violations = techObjectManager.TechObjects
                .Where(o => o.BaseTechObject?.IsSingleInstance == true)
                .GroupBy(o => o.BaseTechObject.EplanName)
                .Where(g => g.Count() > 1)
                .Select(g => $"Базовый объект \"{g.First().BaseTechObject.Name}\" " +
                    $"({g.Key}) допускает только один экземпляр, " +
                    $"создано: {g.Count()} (объекты №{string.Join(", №", g.Select(o => techObjectManager.GetTechObjectN(o)))})\n");

            var genericViolations = techObjectManager.GenericTechObjects
                .Where(o => o.BaseTechObject?.IsSingleInstance == true)
                .GroupBy(o => o.BaseTechObject.EplanName)
                .Where(g => g.Count() > 1)
                .Select(g => $"Базовый объект \"{g.First().BaseTechObject.Name}\" " +
                    $"({g.Key}) допускает только один типовой экземпляр, " +
                    $"создано: {g.Count()} (типовые объекты №{string.Join(", №", g.Select(o => techObjectManager.GetGenericObjectN(o)))})\n");

            return string.Join("", violations.Concat(genericViolations));
        }

        private ITechObjectManager techObjectManager;
    }
}
