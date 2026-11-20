# RaceDay – API Endpoint Plan (Part 1, Section B)

Authentication is session-based: login stores `UserId` and `UserRole` in the server-side session. Passwords are hashed before storage.
Role values: **None** (public), **Any** (logged in), **Organiser**, **Participant**.
Organiser endpoints that touch an event also check the event belongs to the logged-in Organiser (403 otherwise).

## Authentication
| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates an account with a chosen role (Organiser or Participant); password is hashed. | None | { firstName, lastName, email, password, phoneNumber?, dateOfBirth?, role } | 201 Created – userId; 400 Bad Request – invalid role/data; 409 Conflict – email already registered |
| POST | /api/auth/login | Verifies credentials and starts a session storing user ID and role. | None | { email, password } | 200 OK – user details and role; 401 Unauthorized – invalid email or password |
| POST | /api/auth/logout | Ends the session. | Any | None | 204 No Content |

## User Profile
| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/profile | Returns the logged-in user's profile. | Any | None | 200 OK – profile; 401 Unauthorized; 404 Not Found |
| PUT | /api/users/profile | Updates the logged-in user's own profile details. | Any | { firstName, lastName, phoneNumber?, dateOfBirth? } | 200 OK; 400 Bad Request; 401 Unauthorized |
| POST | /api/users/profile-picture | Uploads a profile picture to Azure Blob Storage and saves its URL (Part 3). | Any | multipart form: file | 200 OK – imageUrl; 400 Bad Request – no file; 401 Unauthorized |

## Events
| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all events. | Any | None | 200 OK – event list; 401 Unauthorized |
| GET | /api/events/{id} | Returns one event with its categories. | Any | None | 200 OK; 401; 404 Not Found |
| GET | /api/events/mine | Lists events created by the logged-in Organiser. | Organiser | None | 200 OK; 401; 403 Forbidden |
| POST | /api/events | Creates an event (optional banner image). | Organiser | { name, description, eventDate, location, distanceKm, eventType (Run/Walk/Cycle), bannerImage? } | 201 Created – eventId; 400; 401; 403 |
| PUT | /api/events/{id} | Updates one of the Organiser's own events; an optional new banner image replaces the old one. | Organiser | multipart form: { name, description, eventDate, location, distanceKm, eventType, bannerImage? } | 200 OK; 401; 403 – not your event; 404 |
| DELETE | /api/events/{id} | Deletes one of the Organiser's own events (categories cascade). | Organiser | None | 204 No Content; 401; 403; 404; 409 Conflict – participants enrolled |

## Categories
| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists the categories of an event. | Any | None | 200 OK; 401; 404 – event not found |
| POST | /api/events/{eventId}/categories | Adds an age or distance category to the Organiser's own event. | Organiser | { name, categoryType (Age/Distance), minAge?, maxAge?, distanceKm? } | 201 Created – categoryId; 401; 403; 404; 409 – duplicate name |
| PUT | /api/categories/{id} | Updates a category on the Organiser's own event. | Organiser | { name, categoryType, minAge?, maxAge?, distanceKm? } | 200 OK; 401; 403; 404 |
| DELETE | /api/categories/{id} | Removes a category from the Organiser's own event. | Organiser | None | 204 No Content; 401; 403; 404; 409 – participants enrolled |

## Event Enrolments
| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/events/{eventId}/enrolments | Participant enters an event by choosing a category; status starts as Pending. | Participant | { categoryId } | 201 Created – enrolmentId; 400 – category not in event; 401; 403; 404; 409 – already enrolled |
| GET | /api/enrolments/mine | Lists the logged-in Participant's own enrolments and statuses. | Participant | None | 200 OK; 401; 403 |
| GET | /api/events/{eventId}/enrolments | Organiser views all enrolments for their own event. | Organiser | None | 200 OK; 401; 403; 404 |
| PUT | /api/enrolments/{id}/status | Organiser sets enrolment status (Pending/Confirmed/Cancelled). | Organiser | { status } | 200 OK; 400 – invalid status; 401; 403; 404 |
| DELETE | /api/enrolments/{id} | Participant withdraws their own enrolment. | Participant | None | 204 No Content; 401; 403; 404 |

## Results
| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/events/{eventId}/results | Organiser captures finish time and position for an enrolment. | Organiser | { enrolmentId, finishTime, finishPosition } | 201 Created – resultId; 400 – invalid position or cancelled enrolment; 401; 403; 404; 409 – result already captured or position already awarded |
| PUT | /api/results/{id} | Organiser corrects a captured result. | Organiser | { finishTime, finishPosition } | 200 OK; 401; 403; 404 |
| GET | /api/events/{eventId}/results | Organiser views all results for their own event. | Organiser | None | 200 OK; 401; 403; 404 |
| GET | /api/results/mine | Participant views their own result history (event, date, category, finish time, position, total finishers). | Participant | None | 200 OK; 401; 403 |

