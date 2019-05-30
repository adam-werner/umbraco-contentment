﻿/* Copyright © 2019 Lee Kelleher, Umbrella Inc and other contributors.
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.IO;
using Umbraco.Core.PropertyEditors;

namespace Our.Umbraco.Contentment.DataEditors
{
    public class ConfigurationEditorConfigurationEditor : ConfigurationEditor
    {
        public ConfigurationEditorConfigurationEditor()
            : base()
        {
            var configEditors = GetConfigurationEditors<IConfigurationEditorItem>(ignoreFields: true);
            var items = new List<DataListItemModel>();
            foreach (var configEditor in configEditors)
            {
                items.Add(new DataListItemModel
                {
                    Icon = configEditor.Icon,
                    Name = configEditor.Name,
                    Description = configEditor.Description,
                    Value = configEditor.Type
                });
            }

            Fields.Add(
                Constants.Conventions.ConfigurationEditors.Items,
                "Items",
                "Select the configuration editors to use.",
                IOHelper.ResolveUrl(ItemPickerDataEditor.DataEditorViewPath),
                new Dictionary<string, object>
                {
                    { Constants.Conventions.ConfigurationEditors.Items, items },
                    { "allowDuplicates", Constants.Values.False }
                });

            Fields.Add(
                "enableFilter",
                "Enable search filter?",
                "Select to enable the search filter in the overlay selection panel.",
                "boolean");

            Fields.Add(
                "overlaySize",
                "Overlay size",
                "Select the size of the overlay editing panel. By default this is set to 'large'. However if the configuration editor fields require a smaller panel, select 'small'.",
                IOHelper.ResolveUrl(RadioButtonListDataEditor.DataEditorViewPath),
                new Dictionary<string, object>
                {
                    {
                        Constants.Conventions.ConfigurationEditors.Items, new[]
                        {
                            new { name = "Small", value = "small" },
                            new { name = "Large", value = "large" }
                        }
                    },
                    { Constants.Conventions.ConfigurationEditors.DefaultValue, "large" },
                    { "orientation", "horizontal" }
                });

            Fields.AddMaxItems();
            Fields.AddDisableSorting();
            Fields.AddHideLabel();
        }

        public override IDictionary<string, object> ToValueEditor(object configuration)
        {
            var config = base.ToValueEditor(configuration);

            if (config.TryGetValue(Constants.Conventions.ConfigurationEditors.Items, out var items) && items is JArray array && array.Count > 0)
            {
                var types = new List<Type>();

                foreach (var item in array)
                {
                    // TODO: I should try to use `TypeLoader` here. I'm unsure how do to DI here. [LK]
                    var type = TypeFinder.GetTypeByName(item.Value<string>());
                    if (type != null)
                    {
                        types.Add(type);
                    }
                }

                config["items"] = GetConfigurationEditors<IConfigurationEditorItem>(types);
            }

            return config;
        }

        // TODO: Review if these methods should be in a "Service" or other class? Feels odd them being in here. [LK]
        private static IEnumerable<ConfigurationEditorModel> GetConfigurationEditors<TConfigurationEditor>(IEnumerable<Type> types, bool ignoreFields = false)
            where TConfigurationEditor : class, IConfigurationEditorItem
        {
            if (types == null)
                return Array.Empty<ConfigurationEditorModel>();

            var models = new List<ConfigurationEditorModel>();

            foreach (var type in types)
            {
                var provider = Activator.CreateInstance(type) as TConfigurationEditor;
                if (provider == null)
                    continue;

                var fields = new List<ConfigurationField>();

                if (ignoreFields == false)
                {
                    var properties = type.GetProperties();

                    foreach (var property in properties)
                    {
                        if (Attribute.IsDefined(property, typeof(ConfigurationFieldAttribute)) == false)
                            continue;

                        var attr = property.GetCustomAttribute<ConfigurationFieldAttribute>(false);
                        if (attr == null)
                            continue;

                        if (attr.Type != null)
                        {
                            var field = Activator.CreateInstance(attr.Type) as ConfigurationField;
                            if (field != null)
                            {
                                fields.Add(field);
                            }
                        }
                        else
                        {
                            fields.Add(new ConfigurationField
                            {
                                Key = attr.Key ?? property.Name,
                                Name = attr.Name ?? property.Name,
                                PropertyName = property.Name,
                                PropertyType = property.PropertyType,
                                Description = attr.Description,
                                HideLabel = attr.HideLabel,
                                View = attr.View
                            });
                        }
                    }
                }

                models.Add(new ConfigurationEditorModel
                {
                    Type = type.GetFullNameWithAssembly(),
                    Name = provider.Name ?? type.Name.SplitPascalCasing(),
                    Description = provider.Description,
                    Icon = provider.Icon ?? "icon-science",
                    Fields = fields
                });
            }

            return models;
        }

        internal static IEnumerable<ConfigurationEditorModel> GetConfigurationEditors<TConfigurationEditor>(bool ignoreFields = false)
            where TConfigurationEditor : class, IConfigurationEditorItem
        {
            // TODO: I should try to use `TypeLoader` here. I'm unsure how do to DI here. [LK]
            return GetConfigurationEditors<TConfigurationEditor>(TypeFinder.FindClassesOfType<TConfigurationEditor>(), ignoreFields);
        }
    }
}
