# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

- **Start development server**: `npm run serve` (runs on port 3001)
- **Build for production**: `npm run build` (outputs to `../../KZ-Server-side/KZ/Scripts`)
- **Lint code**: `npm run lint`
- **Install dependencies**: `npm install`

## Project Architecture

This is a Vue.js 2 application for the **Kaunas Street Signs Management Information System** (Kauno miesto techninių eismo reguliavimo priemonių IS). The system manages traffic regulation equipment including street signs, their placement, approval workflows, and maintenance.

### Key Technologies
- Vue.js 2.6 with Vue CLI 5
- Vuetify 2.7 for UI components
- OpenLayers 6 for interactive mapping
- Vuex for state management
- Vue Router for navigation
- Multiple mapping/GIS libraries: Google Maps API, Turf.js, Proj4, Fabric.js

### Application Structure

The app is configured as a multi-page application with an admin interface:
- **Main entry point**: `src/admin-app/main.js`
- **Router configuration**: `src/admin-app/router.js`
- **State management**: `src/admin-app/store.js`
- **Development server**: Proxies API calls to `https://localhost:44397/`

### Core Domain Concepts

1. **Street Signs Management**
   - Horizontal signs (points, polylines, polygons)
   - Vertical signs with supports
   - Custom symbol creation and management
   - Approval/rejection workflows

2. **Tasks System**
   - Task creation and delegation
   - Feature modification tracking (add/update/delete)
   - Master attachments and comments
   - Status management and history

3. **Mapping & GIS**
   - Interactive map interface with multiple layers
   - Address search integration
   - Measurement tools and area calculations
   - Export functionality (maps, statistics)
   - Panorama viewer integration

### Key Components Structure

- **Main Components** (`src/components/main/`): Core application screens
  - `AdminMap.vue` - Main map interface
  - `LoginForm.vue` - Authentication
  - `StreetSignsSymbolsManager.vue` - Symbol management
  - `UsersManager.vue` - User administration

- **Map Components** (`src/components/over-map/`): Map overlay functionality
  - Address search, file upload, layer management
  - Measurement tools, statistics generation
  - Map export capabilities

- **Feature Management** (`src/components/`): Street sign and task management
  - Feature popups, photo management, history tracking
  - Task creation, delegation, comments
  - Attribute editing and validation

- **Helpers** (`src/components/helpers/`): Utility components
  - `CommonHelper.vue` - Central configuration and utilities
  - `MapHelper.vue`, `TaskHelper.vue` - Domain-specific helpers
  - Style helpers for map layers

### State Management (Vuex Store)

Key state properties:
- `myMap`: OpenLayers map instance
- `activeFeature`: Currently selected map feature
- `activeTask`: Current task being worked on
- `userData`: User authentication and permissions
- `layersInfoDict`: Map layer configuration

### API Integration

- **Proxy configuration**: Routes `/kauno_eop_is/(web-services|Proxy)/` to backend
- **ArcGIS Services**: Integration with Esri FeatureServer endpoints
- **Custom services**: Symbol management, statistics, geometry processing

### Development Notes

- Uses Vue CLI service with custom webpack configuration
- Production builds disable source maps and optimize chunk naming
- CSS extraction is disabled for inline styles
- Development mode includes test delays for UI simulation