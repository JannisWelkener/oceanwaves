#version 430

layout(local_size_x = 64) in;  // Dispatch work groups of 64 threads

// Output array for visible instance IDs
/*
layout(std430, binding = 0) buffer CulledInstances
{
    uint visibleInstanceIDs[];  // Array to store visible instance IDs
};
*/

uniform uint numInstances;         // Total number of instances
uniform vec3 startPosition;        // Starting position for instances
uniform mat4 viewProjectionMatrix; // View-projection matrix for frustum check

layout(binding = 0, offset = 0) uniform atomic_uint visibleCount;

void main() {
    /*
    uint id = gl_GlobalInvocationID.x;  // Original instance ID (thread ID)
    vec2 posv2 = vec2((int(id / 64)) / 64.0f,((id % 64)) / 64.0f);

    vec4 instancePos = vec4(posv2.x,-1,posv2.y,1); //is one correct??
    vec4 clipPos = viewProjectionMatrix * instancePos;

    // Simple frustum culling logic (sphere test)
    if (clipPos.x > -clipPos.w && clipPos.x < clipPos.w &&
        clipPos.y > -clipPos.w && clipPos.y < clipPos.w &&
        clipPos.z > 0.0 && clipPos.z < clipPos.w) {
        
        //uint visibleIndex = atomicAdd(instanceCount, 1);  // Increment visible instance count atomically
        uint visibleIndex = atomicCounterIncrement(visibleCount);
        
        // Store the original instance ID in the visibleInstanceIDs buffer
        visibleInstanceIDs[visibleIndex] = id;
    }
    */
    //atomicAdd(instanceCount, 64*64);
    if(gl_GlobalInvocationID.x % 2 == 0)
    atomicCounterIncrement(visibleCount);
}