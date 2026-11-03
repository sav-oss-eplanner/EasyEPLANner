using EasyEPlanner.Extensions;
using Eplan.EplApi.Base;
using Eplan.EplApi.DataModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace StaticHelper
{
    /// <summary>
    /// <inheritdoc cref="IEplanFunction"/>
    /// </summary>
    /// <param name="function"></param>
    [ExcludeFromCodeCoverage]
    public class EplanFunction(Function function) : IEplanFunction
    {
        public Function Function => function;

        public string IP 
        {
            get => function.Properties.FUNC_PLCGROUP_STARTADDRESS.GetString();
            set => function.Properties.FUNC_PLCGROUP_STARTADDRESS = value;
        }

        public string SubnetMask 
        { 
            get => function.Properties.FUNC_PLC_SUBNETMASK.GetString(); 
            set => function.Properties.FUNC_PLC_SUBNETMASK = value;
        }

        public string Gateway 
        { 
            get => GetSupplemenataryField(SuppField.Gateway); 
            set => SetSupplementaryField(SuppField.Gateway, value); 
        }

        public int ClampNumber => int.TryParse(
            function.Properties.FUNC_ADDITIONALIDENTIFYINGNAMEPART.GetString(),
            out int clamp) ? clamp : -1;

        public string VisibleName => function.VisibleName;

        public IEnumerable<IEplanFunction> SubFunctions => function.SubFunctions.Select(f => new EplanFunction(f));

        public string FunctionalText 
        { 
            get => function.GetFunctionalText();
            set => function.Properties.FUNC_TEXT = value;
        }

        public string Name => function.Name;

        public bool PlacedOnCircuit => function.Page.PageType is DocumentTypeManager.DocumentType.Circuit;

        public string GetSupplemenataryField(int propertyIndex)
            => function.Properties.FUNC_SUPPLEMENTARYFIELD[propertyIndex].GetString();

        public void SetSupplementaryField(int propertyIndex, string value)
        {
            try
            {
                function.LockObject();
            }
            catch 
            {
                //do nothing
            }
            
            function.Properties.FUNC_SUPPLEMENTARYFIELD[propertyIndex] = value;
        }

        public override bool Equals(object obj) 
            => function == (obj as EplanFunction)?.Function;

        public override int GetHashCode() 
            => function?.GetHashCode() ?? 0;

        public void Lock()
        {
            try
            {
                function.LockObject();
            }
            catch
            {
                //do nothing
            }
        }

        public void Rename(string name)
        {
            Lock();
            function.Name = name;
        }

        public bool IsMainFunction => function.IsMainFunction;

        public bool Expanded 
        {
            get => bool.TryParse(GetSupplemenataryField(SuppField.Expanded), out var res) && res;
            set => SetSupplementaryField(SuppField.Expanded, value.ToString());
        }


        public bool Off
        {
            get => GetSupplemenataryField(SuppField.Off).Trim() == "1";
            set => SetSupplementaryField(SuppField.Off, value ? "1" : "");
        }

        public string SubType
        {
            get => GetSupplemenataryField(SuppField.Subtype);
            set => SetSupplementaryField(SuppField.Subtype, value);
        }

        public string Parameters
        {
            get => GetSupplemenataryField(SuppField.Parameters);
            set => SetSupplementaryField(SuppField.Parameters, value);
        }

        public string Properties
        {
            get => GetSupplemenataryField(SuppField.Properties);
            set => SetSupplementaryField(SuppField.Properties, value);
        }

        public string RuntimeParameters
        {
            get => GetSupplemenataryField(SuppField.RuntimeParameters);
            set => SetSupplementaryField(SuppField.RuntimeParameters, value);
        }

        public string OldDeviceName
        {
            get => GetSupplemenataryField(SuppField.OldDeviceName);
            set => SetSupplementaryField(SuppField.OldDeviceName, value);
        }

        public string Article
        {
            get
            {
                var articleName = string.Empty;
                if (function == null) return articleName;

                var articlesRefs = function.ArticleReferences;
                if (articlesRefs.Length > 0 &&
                    !string.IsNullOrEmpty(function.ArticleReferences[0].PartNr))
                {
                    articleName = function.ArticleReferences[0].PartNr;
                }

                return articleName;
            }
        }

        public bool IsValid => function.IsValid;

        public string Description
        {
            // Логика получения должна совпадать с DeviceHelper.GetDescription,
            // иначе сравнение ФСА <-> main.io.lua даёт ложные отличия
            // (например, "пропадающая" точка из-за разной нарезки строк).
            get
            {
                if (function.Properties.FUNC_COMMENT.IsEmpty)
                    return string.Empty;

                string description = function.Properties.FUNC_COMMENT
                    .ToString(ISOCode.Language.L___);

                if (description == string.Empty)
                {
                    description = function.Properties.FUNC_COMMENT
                        .ToString(ISOCode.Language.L_ru_RU);
                }

                description = Regex.Replace(description ?? string.Empty,
                    "([\'\"])", string.Empty);

                return description;
            }
            set => function.Properties.FUNC_COMMENT = value;
        }
    }
}
