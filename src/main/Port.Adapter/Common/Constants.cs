using System;
using System.Collections.Generic;
using System.Text;

namespace works.ei8.Cortex.Graph.Port.Adapter.Common
{
    public struct EnvironmentVariableKeys
    {
        public const string EventInfoLogBaseUrl = @"EVENT_INFO_LOG_BASE_URL";
        public const string PollInterval = @"POLL_INTERVAL";
        public const string DbName = @"DB_NAME";
        public const string DbUrl = @"DB_URL";
        public const string DbUsername = @"DB_USERNAME";
        public const string DbPassword = @"DB_PASSWORD";
        public const string RequireAuthentication = "REQUIRE_AUTHENTICATION";
        public const string TokenIssuerAddress = "TOKEN_ISSUER_ADDRESS";
    }
}
