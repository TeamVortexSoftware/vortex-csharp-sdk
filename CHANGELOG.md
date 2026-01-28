# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.0] - 2026-01-23

### Added
- **Internal Invitations**: New `'internal'` delivery type for customer-managed invitations
  - Support for `DeliveryTypes: ["internal"]`
  - No email/SMS communication triggered by Vortex
  - Target value can be any customer-defined identifier
  - Useful for in-app invitation flows managed by customer's application

### Changed
- Updated `DeliveryTypes` property documentation to include `'internal'` as a valid value

## [1.1.3] - 2025-01-29

### Added
- **AcceptUser Class**: New preferred format for accepting invitations with `Email`, `Phone`, and `Name` properties
- Enhanced `AcceptInvitations` method to support both new User format and legacy target format

### Changed
- **DEPRECATED**: Legacy `InvitationTarget` format for `AcceptInvitations` - use `AcceptUser` instead
- Internal API calls now always use User format for consistency
- Added `[Obsolete]` attribute and warning messages for legacy target format

### Fixed
- Maintained 100% backward compatibility - existing code using legacy target format continues to work

## [1.0.1] - 2025-01-31

### Fixed
- **CRITICAL FIX**: JWT generation now produces identical output to Node.js SDK
  - Fixed UUID decoding to use big-endian byte order (was using `Guid` constructor which has mixed endianness)
  - Fixed JWT payload property order to match Node.js SDK for signature compatibility
  - Property order now: userId, groups, role, expires, identifiers (was: userId, identifiers, groups, role, expires)
- Added comprehensive tests verifying JWT output matches Node.js SDK byte-for-byte

### Breaking Changes
- JWT structure changed - tokens from 1.0.0 are incompatible with 1.0.1
- JWTs from 1.0.1 will now correctly validate across all Vortex SDKs

## [1.0.0] - 2024-10-10

### Added
- Initial release of Vortex C# SDK
- JWT generation with HMAC-SHA256 signing
- Complete invitation management API
- Support for .NET 6.0+
- Type-safe models with JSON serialization
- Async API methods
- Context manager support with IDisposable
- Comprehensive error handling with VortexException
- Full compatibility with Node.js SDK API
