# RaceDay - API Endpoint Plan (Part 1, Section B)

This table lists every endpoint the RaceDay API will expose in Part 2. No API code
has been written for Part 1 - this is a plan only.

**Role key:** `None` = public, `Any` = any logged-in user, `Organiser` / `Participant` = that role only.

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Registers a new user as an Organiser or Participant. | None | `{ fullName, email, password, role }` | 201 Created - new user (no password) returned. 400 Bad Request - validation failed. 409 Conflict - email already registered. |
| POST | /api/auth/login | Authenticates a user and returns a JWT access token. | None | `{ email, password }` | 200 OK - JWT token and user role. 401 Unauthorized - invalid credentials. |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/me | Returns the profile of the currently logged-in user. | Any | None | 200 OK - user profile. 401 Unauthorized - no/invalid token. |
| PUT | /api/users/me | Updates the logged-in user's own profile details. | Any | `{ fullName, phoneNumber }` | 200 OK - updated profile. 400 Bad Request - validation failed. 401 Unauthorized. |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Lists all upcoming events, so anyone can browse before signing up. | None | None | 200 OK - array of events. |
| GET | /api/events/{id} | Returns full details for a single event. | None | None | 200 OK - event details. 404 Not Found - event does not exist. |
| POST | /api/events | Creates a new event owned by the logged-in Organiser. | Organiser | `{ eventName, description, eventDate, location, city, province }` | 201 Created - new event. 400 Bad Request - validation failed. 401/403 - not logged in / not an Organiser. |
| PUT | /api/events/{id} | Edits an event the Organiser owns. | Organiser | `{ eventName, description, eventDate, location, city, province }` | 200 OK - updated event. 403 Forbidden - not the owning Organiser. 404 Not Found. |
| DELETE | /api/events/{id} | Deletes an event the Organiser owns. | Organiser | None | 204 No Content - deleted. 403 Forbidden - not the owning Organiser. 404 Not Found. |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Lists all race categories under a specific event. | None | None | 200 OK - array of categories. 404 Not Found - event does not exist. |
| POST | /api/events/{eventId}/categories | Adds a new category (e.g. 10km, 21km) to the Organiser's event. | Organiser | `{ categoryName, distanceKm, entryFee, maxParticipants }` | 201 Created - new category. 403 Forbidden - not the owning Organiser. 404 Not Found - event does not exist. |
| PUT | /api/categories/{id} | Edits a category the Organiser owns. | Organiser | `{ categoryName, distanceKm, entryFee, maxParticipants }` | 200 OK - updated category. 403 Forbidden. 404 Not Found. |
| DELETE | /api/categories/{id} | Deletes a category the Organiser owns. | Organiser | None | 204 No Content - deleted. 403 Forbidden. 404 Not Found. |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/categories/{categoryId}/enrolments | Enters the logged-in Participant into a category (generates a race number). | Participant | None (participant identified from the token) | 201 Created - enrolment with race number. 404 Not Found - category does not exist. 409 Conflict - already enrolled, or category full. |
| GET | /api/enrolments/me | Returns the logged-in Participant's own enrolment history. | Participant | None | 200 OK - array of the participant's enrolments. |
| GET | /api/events/{eventId}/enrolments | Returns all enrolments for an event, for the owning Organiser to manage on race day. | Organiser | None | 200 OK - array of enrolments. 403 Forbidden - not the owning Organiser. 404 Not Found - event does not exist. |
| DELETE | /api/enrolments/{id} | Cancels the logged-in Participant's own enrolment. | Participant | None | 204 No Content - cancelled. 403 Forbidden - not the owning participant. 404 Not Found. |

## Payments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/payments | Creates a payment record for the participant's own enrolment (pays the category's entry fee). | Participant | `{ amount, paymentMethod }` | 201 Created - payment record. 403 Forbidden - not the owning participant. 404 Not Found - enrolment does not exist. 409 Conflict - enrolment already paid. |
| GET | /api/payments/me | Returns the logged-in Participant's own payment history. | Participant | None | 200 OK - array of the participant's payments. |
| GET | /api/events/{eventId}/payments | Returns all payments for an event's enrolments, for the owning Organiser (revenue tracking). | Organiser | None | 200 OK - array of payments. 403 Forbidden - not the owning Organiser. 404 Not Found - event does not exist. |
| PUT | /api/payments/{id} | Updates a payment's status (e.g. confirms it as Paid once processed). | Organiser | `{ paymentStatus, transactionReference }` | 200 OK - updated payment. 403 Forbidden. 404 Not Found. |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/results | Captures a finish result for an enrolment (Organiser, after the race). | Organiser | `{ finishTime, position, status }` | 201 Created - new result. 403 Forbidden - not the event's Organiser. 404 Not Found - enrolment does not exist. |
| PUT | /api/results/{id} | Corrects a previously captured result. | Organiser | `{ finishTime, position, status }` | 200 OK - updated result. 403 Forbidden. 404 Not Found. |
| GET | /api/results/me | Returns the logged-in Participant's personal performance history across events. | Participant | None | 200 OK - array of the participant's results. |
| GET | /api/events/{eventId}/results | Returns the public leaderboard/results for an event. | None | None | 200 OK - array of results. 404 Not Found - event does not exist. |

---
*This plan will be closely followed when the API is implemented in Part 2. Any deviation
will be explained in the Part 2 README.*
