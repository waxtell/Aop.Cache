# Aop.Cache
Simple, method centric, AOP cache adapter.

Basic functionality only at present.
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
