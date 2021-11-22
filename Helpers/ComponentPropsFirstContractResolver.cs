using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Altinn2Convert.Helpers
{
    /// <summary>Custom contract resolver that places the common component props first in the json output</summary>
    public class ComponentPropsFirstContractResolver : DefaultContractResolver
    {
        /// <summary>
        /// Creates properties for the given <see cref="JsonContract"/>.
        /// </summary>
        /// <param name="type">The type to create properties for.</param>
        /// /// <param name="memberSerialization">The member serialization mode for the type.</param>
        /// <returns>Properties for the given <see cref="JsonContract"/>.</returns>
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            return base.CreateProperties(type, memberSerialization).OrderBy(p => p.DeclaringType != typeof(Altinn2Convert.Models.Altinn3.layout.Component)).ToList();
        }
    }
}
