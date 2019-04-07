﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Steps
{
    /// <summary>
    /// Deletes a property from memory
    /// </summary>
    public class DeleteProperty : DialogCommand
    {
        [JsonConstructor]
        public DeleteProperty([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0) : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        public DeleteProperty(string property, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
            if (!string.IsNullOrEmpty(property))
            {
                this.Property = property;
            }
        }

        protected override async Task<DialogTurnResult> OnRunCommandAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Ensure planning context
            if (dc is PlanningContext planning)
            {
                dc.State.SetValue(Property, null);
                return await dc.EndDialogAsync();
            }
            else
            {
                throw new Exception("`ClearProperty` should only be used in the context of an adaptive dialog.");
            }
        }
    }
}
