#!/bin/bash

# Docker build script for multi-platform deployment
# This script builds the Docker image for linux/amd64 platform (required for Render deployment)

set -e

# Show help
if [[ "$1" == "--help" || "$1" == "-h" ]]; then
    echo "Usage: $0 [TAG] [--push]"
    echo ""
    echo "  TAG     Docker image tag (default: mongodb)"
    echo "  --push  Push image to registry after building"
    echo ""
    echo "Examples:"
    echo "  $0                    # Build with 'mongodb' tag"
    echo "  $0 latest             # Build with 'latest' tag"
    echo "  $0 mongodb --push     # Build and push"
    exit 0
fi

IMAGE_NAME="doncorleone/laundrysignalr"
TAG=${1:-mongodb}
PLATFORM="linux/amd64"

echo "Building Docker image: ${IMAGE_NAME}:${TAG}"
echo "Platform: ${PLATFORM}"

# Build the image
docker build --platform "${PLATFORM}" -t "${IMAGE_NAME}:${TAG}" .

echo "Build completed successfully!"
echo "To push the image, run: docker push ${IMAGE_NAME}:${TAG}"

# Optionally push immediately
if [[ "$2" == "--push" ]]; then
    echo "Pushing image to registry..."
    docker push "${IMAGE_NAME}:${TAG}"
    echo "Push completed!"
fi