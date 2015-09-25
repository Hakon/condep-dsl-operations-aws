using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;
using ConDep.Dsl.Logging;

namespace ConDep.Dsl.Operations.Aws.Ec2.Handlers
{
    internal class Ec2InstancePasswordHandler
    {
        private readonly IAmazonEC2 _client;

        public Ec2InstancePasswordHandler(IAmazonEC2 client)
        {
            _client = client;
        }

        //Todo: Add parameter for rsa key
        public List<Tuple<string, string>> WaitForPassword(IEnumerable<string> instanceIds, string pathToRsaKey)
        {
            var key = ReadKeyFromFile(pathToRsaKey);

            var tasks = instanceIds.Select(id => Task.Factory.StartNew(() => WaitForPassword((string) id, key))).ToList();
            Task.WaitAll(tasks.ToArray());

            return tasks.Select(x => x.Result).ToList();
        }

        private string ReadKeyFromFile(string pathToRsaKey)
        {
            if (!File.Exists(pathToRsaKey))
                throw new FileNotFoundException(pathToRsaKey);

            return File.ReadAllText(pathToRsaKey);
        }

        public Tuple<string, string> WaitForPassword(string instanceId, string key)
        {
            var passwordRequest = new GetPasswordDataRequest { InstanceId = instanceId };

            var response = _client.GetPasswordData(passwordRequest);
            if (string.IsNullOrWhiteSpace(response.PasswordData))
            {
                Logger.Info(string.Format("Password not yet ready for {0}, waiting 30 seconds...", instanceId));
                Thread.Sleep(30000);
                return WaitForPassword(instanceId, key);
            }
            return new Tuple<string, string>(instanceId, response.GetDecryptedPassword(key));
        }
    }
}