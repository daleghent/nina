#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using NINA.Sequencer.Conditions;
using Newtonsoft.Json.Linq;

namespace NINA.Sequencer.Serialization {

    public class SequenceConditionCreationConverter : JsonCreationConverter<ISequenceCondition> {
        private ISequencerFactory factory;

        public SequenceConditionCreationConverter(ISequencerFactory factory) {
            this.factory = factory;
        }

        public override ISequenceCondition Create(Type objectType, JObject jObject) {
            var t = GetType(jObject.GetValue("$type").ToString());
            var method = factory.GetType().GetMethod(nameof(factory.GetCondition)).MakeGenericMethod(new Type[] { t });
            var obj = method.Invoke(factory, null);
            return (ISequenceCondition)obj;
        }
    }
}