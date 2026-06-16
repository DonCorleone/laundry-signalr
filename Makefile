# Docker commands for LaundrySignalR
IMAGE_NAME = doncorleone/laundrysignalr
TAG = mongodb
PLATFORM = linux/amd64

.PHONY: build push deploy clean help

# Build Docker image for deployment platform
build:
	@echo "Building Docker image for $(PLATFORM)..."
	docker build --platform $(PLATFORM) -t $(IMAGE_NAME):$(TAG) .
	@echo "Build completed: $(IMAGE_NAME):$(TAG)"

# Push Docker image to registry
push:
	@echo "Pushing $(IMAGE_NAME):$(TAG) to registry..."
	docker push $(IMAGE_NAME):$(TAG)
	@echo "Push completed!"

# Build and push in one command
deploy: build push
	@echo "Deploy completed: $(IMAGE_NAME):$(TAG)"

# Clean up local Docker images
clean:
	@echo "Cleaning up Docker images..."
	docker rmi $(IMAGE_NAME):$(TAG) || true
	docker image prune -f

# Show available commands
help:
	@echo "Available commands:"
	@echo "  make build  - Build Docker image for $(PLATFORM)"
	@echo "  make push   - Push Docker image to registry"
	@echo "  make deploy - Build and push Docker image"
	@echo "  make clean  - Clean up local Docker images"
	@echo "  make help   - Show this help message"
	@echo ""
	@echo "Current settings:"
	@echo "  Image: $(IMAGE_NAME):$(TAG)"
	@echo "  Platform: $(PLATFORM)"