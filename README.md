# Aop.Cache
Simple, method centric, AOP cache adapter.

Basic functionality only at present.

[![Build status](https://ci.appveyor.com/api/projects/status/hxyxeqsgos31dhh7?svg=true)](https://ci.appveyor.com/project/waxtell/aop-cache) [![NuGet Badge](https://buildstats.info/nuget/Aop.Cache)](https://www.nuget.org/packages/Aop.Cache/)

```csharp
                var proxy = new PerMethodAdapter<ITokenClient>(client);

                proxy
                    .Cache
                    (
                        x => x.FromPassword
                        (
                            It.IsAny<string>(),  // username
                            It.IsAny<string>(),  // password
                            It.IsAny<string[]>() // scopes
                        ),
                        While
                            .Result
                            .True<TokenClient.Token>
                            (
                                (token, executionUtc) => DateTime.UtcNow < executionUtc.AddSeconds(token.ExpirationInSeconds)
                            )
                    );

                return proxy.Object;
```
