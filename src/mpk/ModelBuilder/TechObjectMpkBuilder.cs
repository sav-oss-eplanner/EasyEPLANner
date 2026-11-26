using EasyEPlanner.mpk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TechObject;

namespace EasyEPlanner.mpk.ModelBuilder
{
    public class TechObjectMpkBuilder
    {
        private readonly ITechObjectManager techObjectManager;

        private readonly string containerName;

        public TechObjectMpkBuilder(ITechObjectManager techObjectManager, string containerName) 
        {
            this.techObjectManager = techObjectManager;
            this.containerName = containerName;
        }

        public IContainer Build()
        {
            var objects = techObjectManager.TechObjects
                .Select(to => new
                {
                    ChannelBaseName = Regex.Replace(to.GetNameForChannelBase(), @"\d+$", "", RegexOptions.None, TimeSpan.FromMilliseconds(100)),
                    TechObject = to,
                })
                .Where(item => item.ChannelBaseName != "")
                .GroupBy(item => item.ChannelBaseName)
                .ToDictionary(group => group.Key, group => group.First().TechObject);
            var container = new Container()
            {
                Name = containerName,
            };

            container.Components.Add(new Component()
            {
                Name = "SYSTEM",
                Properties = GetSystemProperties(),
            });

            foreach (var comp in objects)
            {
                if (comp.Value is null)
                {
                    Logs.AddMessage($"Не удалось определить технологический объект по ключу '{comp.Key}'\n");
                    continue;
                }

                container.Components.Add(new Component
                {
                    Name = comp.Key,
                    Properties = [
                        .. GetBaseProperties(),
                        .. GetTechObjectProperties(comp.Value)
                        ]
                });
            }

            return container;
        }


        public List<IProperty> GetTechObjectProperties(TechObject.TechObject techObject)
        {
            return
            [
                .. GetOperationProperties(techObject.ModesManager.Modes),
                .. GetParametersProperties(techObject.GetParamsManager()?.Float, PropertyType.Float, "F"),
            ];
        }

        public List<IProperty> GetOperationProperties(List<Mode> operations)
        {
            return [.. operations.SelectMany<Mode, Property>(op => [ 
                new Property()
                {
                    Name = $"mode{op.GetModeNumber()}",
                    Caption =  $"mode{op.GetModeNumber()} ({op.Name})",
                    PropModel = PropertyModel.Tag,
                    PropType = PropertyType.Integer,
                },
                new Property()
                {
                    Name = $"OPERATIONS{op.GetModeNumber()}",
                    Caption =  $"OPERATIONS{op.GetModeNumber()}",
                    PropModel = PropertyModel.Tag,
                    PropType = PropertyType.Integer,
                },
                new Property()
                {
                    Name = $"step{op.GetModeNumber()}",
                    Caption =  $"step{op.GetModeNumber()}",
                    PropModel = PropertyModel.Tag,
                    PropType = PropertyType.Integer,
                },
            ]).OrderBy(p => p.Name)];
        }

        public List<IProperty> GetParametersProperties(Params parameters, PropertyType type, string shortTag)
        {
            return [.. parameters?.Parameters?.Select(p => new Property()
            {
                Name = $"{parameters.NameForChannelBase}{p.GetParameterNumber}",
                Caption = $"{shortTag}{p.GetParameterNumber} {p.GetName()}, {p.GetMeter()}",
                PropModel = PropertyModel.Tag,
                PropType = type,
            })];
        }

        public List<IProperty> GetBaseProperties()
        {
            return [
                new Property("cmd", PropertyModel.Tag, PropertyType.Integer),
                new Property("Ladder", PropertyModel.Global, PropertyType.Integer) { Report = true },
                new Property("Nmr", PropertyModel.Local, PropertyType.Integer),
                new Property("ST", PropertyModel.Tag, PropertyType.Integer),
                new Property("ST_Str", PropertyModel.Local, PropertyType.String),
                new Property("STEX_STR", PropertyModel.Local, PropertyType.String),
                new Property("Step_Str", PropertyModel.Local, PropertyType.String),
                new Property("Step_Time_Str", PropertyModel.Local, PropertyType.String),
                new Property("Time_Str", PropertyModel.Local, PropertyType.String),
                new Property("WorkCenter", PropertyModel.Global, PropertyType.Integer),
                ];
        }

        public List<IProperty> GetSystemProperties()
        {
            return [
                new Property("CMD", PropertyModel.Tag, PropertyType.Integer) { Priority = 10 },
                new Property("CMD_ANSWER", PropertyModel.Tag, PropertyType.String),
                new Property("PAUSE", PropertyModel.Tag, PropertyType.Integer) { Priority = 10 },
                new Property("REST_MAN_TIME", PropertyModel.Tag, PropertyType.Integer) 
                { 
                    Caption = "REST_MAN", 
                    Priority = 10 
                },
                new Property("REST_MODE", PropertyModel.Tag, PropertyType.Integer) { Priority = 10 },
                new Property("SP1", PropertyModel.Tag, PropertyType.Integer)
                {
                    Caption = "SP1 Интервал промывки седел клапанов, сек",
                },
                new Property("SP2", PropertyModel.Tag, PropertyType.Integer)
                {
                    Caption = "SP2 Задержка выключения клапанов, мсек",
                },
                new Property("SP3", PropertyModel.Tag, PropertyType.Integer)
                {
                    Caption = "SP3 Время флипа верхних седел клапанов, мсек",
                },
                new Property("SP4", PropertyModel.Tag, PropertyType.Integer)
                {
                    Caption = "SP4 Время флипа нижних седел клапанов, мсек",
                },
                new Property("UP_TIME", PropertyModel.Tag, PropertyType.String),
                ];
        }
    }
}
