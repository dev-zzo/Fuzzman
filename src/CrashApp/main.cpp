int main()
{
	int *bad_ptr = (int *)0x41414141;
#if 0
	*bad_ptr = 0x12345678;
	
	return 0;
#endif
#if 1
	return *bad_ptr;
#endif
}
