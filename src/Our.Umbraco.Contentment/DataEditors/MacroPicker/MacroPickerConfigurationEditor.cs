﻿/* Copyright © 2019 Lee Kelleher, Umbrella Inc and other contributors.
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Umbraco.Core;
using Umbraco.Core.IO;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using UmbracoIcons = Umbraco.Core.Constants.Icons;

namespace Our.Umbraco.Contentment.DataEditors
{
    public class MacroPickerConfigurationEditor : ConfigurationEditor
    {
        private readonly IMacroService _macroService;

        public MacroPickerConfigurationEditor(IMacroService macroService)
            : base()
        {
            _macroService = macroService;

            var macros = macroService.GetAll().Select(x => new DataListItemModel
            {
                Icon = UmbracoIcons.Macro,
                Name = x.Name,
                Description = x.Alias,
                Value = x.GetUdi().ToString()
            });

            Fields.Add(
                "allowedMacros",
                "Allowed macros",
                "Restrict the macros that can be picked.",
                IOHelper.ResolveUrl(ItemPickerDataEditor.DataEditorViewPath),
                new Dictionary<string, object>
                {
                    { Constants.Conventions.ConfigurationEditors.Items, macros }
                });

            Fields.AddMaxItems();
            Fields.AddDisableSorting();
            Fields.AddHideLabel();
        }

        public override IDictionary<string, object> ToValueEditor(object configuration)
        {
            var config = base.ToValueEditor(configuration);

            if (config.TryGetValue("allowedMacros", out var tmp1) && tmp1 is JArray array)
            {
                var ids = new List<Guid>();
                foreach (var token in array)
                {
                    if (GuidUdi.TryParse(token.Value<string>(), out var udi))
                    {
                        ids.Add(udi.Guid);
                    }
                }

                // TODO: Urgh! Send a PR to fix this Umbraco bug with `MacroService.GetAll`. [LK]
                //System.InvalidOperationException: Cannot run a repository without an ambient scope.
                //   at Umbraco.Core.Persistence.Repositories.Implement.RepositoryBase`2.get_AmbientScope()
                //   at Umbraco.Core.Persistence.Repositories.Implement.MacroRepository.GetBaseQuery()
                //   at Umbraco.Core.Persistence.Repositories.Implement.MacroRepository.Get(Guid id)
                // TODO: Commented this out, until after the MacroService bug is fixed.
                //config.Add("availableMacros", _macroService.GetAll(ids).Select(x => x.Alias));
                config.Add("availableMacros", _macroService.GetAll().Where(x => ids.Contains(x.Key)).Select(x => x.Alias));
                config.Remove("allowedMacros");
            }

            return config;
        }
    }
}
