# Client Application Authorization

n-ate Swagger API client applications can authorize using User-Authorization Forwarding (delegated access) which forwards the client application's user's information (this is preferred), or authorization can be done using Application-Level Authorization (app-only access).

It should be noted that direct user authorization on the Swagger UI pages will also function, but should be used during development and testing only. Production environments may not allow users to directly interact with the Swagger UI.

- [Implemention instructions and additional authorization information](https://ablcode.visualstudio.com/Magnetar/_git/techdocs?path=/other-docs/api-authentication/client-api-authorization.md&_a=preview)