#include <openssl/evp.h>
#include <openssl/sha.h>
#include <stdio.h>
#include <string.h>

void print_hash(const unsigned char *hash, size_t length) {
    for (size_t i = 0; i < length; i++) {
        printf("%02x", hash[i]);
    }
    printf("\n");
}

void set_stdout(FILE * ptr)
{
    stdout = ptr;
}

void __errr(const char * str);

int hashstr(const char * input) {
    // Input data to be hashed
    
    __errr(input);
    
    // Output buffer for the hash
    unsigned char hash[SHA512_DIGEST_LENGTH];

    // Initialize OpenSSL library
    OpenSSL_add_all_algorithms();

    // Create and initialize a digest context
    EVP_MD_CTX *mdctx = EVP_MD_CTX_new();
    if (mdctx == NULL) {
        __errr("Error creating digest context");
        fprintf(stderr, "Error creating digest context\n");
        return 1;
    }

    // Select the SHA-256 hashing algorithm
    const EVP_MD *md = EVP_sha512();

    // Initialize the digest context for hashing
    if (EVP_DigestInit_ex(mdctx, md, NULL) != 1) {
        __errr("Error initializing digest");
        fprintf(stderr, "Error initializing digest\n");
        EVP_MD_CTX_free(mdctx);
        return 1;
    }

    // Update the context with the input data
    if (EVP_DigestUpdate(mdctx, input, strlen(input)) != 1) {
        fprintf(stderr, "Error updating digest\n");
        EVP_MD_CTX_free(mdctx);
        return 1;
    }

    // Finalize the hash computation
    unsigned int hash_len;
    if (EVP_DigestFinal_ex(mdctx, hash, &hash_len) != 1) {
        fprintf(stderr, "Error finalizing digest\n");
        EVP_MD_CTX_free(mdctx);
        return 1;
    }

    // Print the resulting hash
    printf("SHA-256 hash of '%s': ", input);
    __errr("Writing hash");
    print_hash(hash, hash_len);

    // Clean up the digest context
    EVP_MD_CTX_free(mdctx);

    // Clean up OpenSSL library
    EVP_cleanup();

    return 0;
}
