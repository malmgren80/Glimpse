using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Glimpse.Core.Configuration;
using Glimpse.Core.Extensibility;

namespace Glimpse.Core.Policy
{
    /// <summary>
    /// Policy which will set Glimpse's runtime policy to <c>Off</c> if a Http response's status code is not on the white list.
    /// </summary>
    public class StatusCodePolicy : IRuntimePolicy, IConfigurable, INeedMyCustomConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusCodePolicy" /> class with an empty white list.
        /// </summary>
        public StatusCodePolicy()
            : this(new List<int>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatusCodePolicy" /> class with the provided <paramref name="statusCodeWhiteList"/>.
        /// </summary>
        /// <param name="statusCodeWhiteList">The status code white list.</param>
        /// <exception cref="System.ArgumentNullException">Exception thrown if <paramref name="statusCodeWhiteList"/> is <c>null</c>.</exception>
        public StatusCodePolicy(IList<int> statusCodeWhiteList)
        {
            if (statusCodeWhiteList == null)
            {
                throw new ArgumentNullException("statusCodeWhiteList");
            }

            StatusCodeWhiteList = statusCodeWhiteList;
            StatusCodeWhiteList.Add(200);
            StatusCodeWhiteList.Add(301);
            StatusCodeWhiteList.Add(302);
        }

        /// <summary>
        /// Gets the point in an Http request lifecycle that a policy should execute.
        /// </summary>
        /// <value>
        /// The moment to execute, <see cref="AjaxPolicy"/> uses <c>EndRequest</c>.
        /// </value>
        public RuntimeEvent ExecuteOn
        {
            get { return RuntimeEvent.EndRequest; }
        }

        /// <summary>
        /// Gets or sets the status code white list.
        /// </summary>
        /// <value>
        /// The status code white list to validate against.
        /// </value>
        public IList<int> StatusCodeWhiteList { get; set; }

        /// <summary>
        /// Executes the specified policy with the given context.
        /// </summary>
        /// <param name="policyContext">The policy context.</param>
        /// <returns>
        /// <c>On</c> if the response status code is contained on the white list, otherwise <c>Off</c>.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">Exception thrown if <paramref name="policyContext"/> is <c>null</c>.</exception>
        public RuntimePolicy Execute(IRuntimePolicyContext policyContext)
        {
            try
            {
                var statusCode = policyContext.RequestMetadata.ResponseStatusCode;
                return StatusCodeWhiteList.Contains(statusCode) ? RuntimePolicy.On : RuntimePolicy.Off;
            }
            catch (Exception exception)
            {
                policyContext.Logger.Warn(Resources.ExecutePolicyWarning, exception, GetType());
                return RuntimePolicy.Off;
            }
        }

        /// <summary>
        /// Provides implementations an instance of <see cref="Section" /> to self populate any end user configuration options.
        /// </summary>
        /// <param name="section">The configuration section, <c>&lt;glimpse&gt;</c> from <c>web.config</c>.</param>
        /// <remarks>
        /// Populates the status code white list with values from <c>web.config</c>.
        /// </remarks>
        /// <example>
        /// Configure the status code white list in <c>web.config</c> with the following entries:
        /// <code>
        /// <![CDATA[
        /// <glimpse defaultRuntimePolicy="On" endpointBaseUri="~/Glimpse.axd">
        ///     <runtimePolicies>
        ///         <statusCodes>
        ///             <!-- <clear /> clear to reset defaults -->
        ///             <add statusCode="{code}" />
        ///         </statusCodes>
        ///     </runtimePolicies>
        /// </glimpse>
        /// ]]>
        /// </code>
        /// </example>
        public void Configure(Section section)
        {
            foreach (StatusCodeElement item in section.RuntimePolicies.StatusCodes)
            {
                AddStatusCode(item.StatusCode);
            }
        }

        public void ProcessCustomConfiguration(CustomConfigurationProvider customConfigurationProvider)
        {
            var statusCodes = customConfigurationProvider.GetMyCustomConfigurationAs<StatusCodePolicyStatusCodes>();
            foreach (var statusCode in statusCodes.StatusCodes)
            {
                AddStatusCode(statusCode.Value);
            }
        }

        private void AddStatusCode(int statusCode)
        {
            if (!StatusCodeWhiteList.Contains(statusCode))
            {
                StatusCodeWhiteList.Add(statusCode);
            }
        }

        [XmlRoot(ElementName = "statusCodes")]
        public class StatusCodePolicyStatusCodes
        {
            [XmlElement(ElementName = "add")]
            public StatusCodePolicyStatusCode[] StatusCodes;
        }

        public class StatusCodePolicyStatusCode
        {
            [XmlAttribute("statusCode")]
            public int Value { get; set; }
        }
    }
}